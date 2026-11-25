#!/usr/bin/env bash
# Signs Sentry's macOS binaries in package-dev/Plugins/macOS with Sentry's Developer ID and notarizes them.
# CI runs it on `main` and `release/*` (see .github/workflows/build.yml) on a Linux x86_64 runner.
#
# Reads the secrets from the environment:
#   APPLE_CERT_DATA      base64 of the Developer ID Application .p12
#   APPLE_CERT_PASSWORD  the .p12's password
#   APPLE_API_KEY        base64 of the App Store Connect API key JSON (rcodesign encode-app-store-connect-api-key)
# Fails when one is missing.
set -euo pipefail

CODESIGN_VERSION="${CODESIGN_VERSION:-0.29.0}"
CODESIGN_CHECKSUM="${CODESIGN_CHECKSUM:-dbe85cedd8ee4217b64e9a0e4c2aef92ab8bcaaa41f20bde99781ff02e600002}"
NOTARY_MAX_WAIT_SECONDS="${NOTARY_MAX_WAIT_SECONDS:-900}"

missing=0
for name in APPLE_CERT_DATA APPLE_CERT_PASSWORD APPLE_API_KEY; do
  if [ -z "${!name:-}" ]; then
    echo "::error title=macOS binaries not signed::Secret $name is not set. Add it to the repository's Actions secrets to sign and notarize the macOS binaries."
    missing=1
  fi
done
if [ "$missing" = 1 ]; then
  exit 1
fi

# rcodesign reads the certificate, its password and the API key only from files. They live in a directory only
# this user can read, removed when the script exits, whether it succeeds or not.
umask 077
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

printf '%s' "$APPLE_CERT_DATA" | base64 --decode > "$work/certs.p12"
printf '%s' "$APPLE_CERT_PASSWORD" > "$work/certs.password"
printf '%s' "$APPLE_API_KEY" | base64 --decode > "$work/apple_key.json"

curl -fsSL "https://github.com/indygreg/apple-platform-rs/releases/download/apple-codesign/${CODESIGN_VERSION}/apple-codesign-${CODESIGN_VERSION}-x86_64-unknown-linux-musl.tar.gz" \
  -o "$work/rcodesign.tar.gz"
echo "${CODESIGN_CHECKSUM}  $work/rcodesign.tar.gz" | sha256sum -c -
tar -xz --strip-components=1 -C "$work" -f "$work/rcodesign.tar.gz"
rcodesign="$work/rcodesign"

cd "$(dirname "$0")/../package-dev/Plugins/macOS"

binaries=(Sentry/Sentry.dylib SentryNative/libsentry-native.dylib SentryNative/SentryCrash.bundle)
for binary in "${binaries[@]}"; do
  "$rcodesign" sign --for-notarization \
    --p12-file "$work/certs.p12" --p12-password-file "$work/certs.password" \
    "$binary"
done

# rcodesign verifies Mach-O files only, so the bundle is checked through its executable.
# The signature info lists every architecture slice.
for binary in Sentry/Sentry.dylib SentryNative/libsentry-native.dylib SentryNative/SentryCrash.bundle/Contents/MacOS/sentry-crash; do
  "$rcodesign" verify "$binary"
  "$rcodesign" print-signature-info "$binary"
done

zip -qr "$work/sentry-macos.zip" "${binaries[@]}"
"$rcodesign" notary-submit --wait --max-wait-seconds "$NOTARY_MAX_WAIT_SECONDS" \
  --api-key-file "$work/apple_key.json" "$work/sentry-macos.zip" 2>&1 | tee "$work/notary.log"
if ! grep -q "Accepted" "$work/notary.log"; then
  echo "::error title=Notarization failed::Apple did not accept the macOS binaries. See the notary log above."
  exit 1
fi
