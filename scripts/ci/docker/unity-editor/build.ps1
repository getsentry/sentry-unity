# Unused: doesn't show any progress from the `docker` command call...


# Param(
#     [string]$version = "2019.4.33f1",
#     [string]$changeSet = "c9b2b02eeeef"
# )

# if (-not($version)) { Throw "You must supply -version" }
# if (-not($changeSet)) { Throw "You must supply -changeSet" }

# # see https://github.com/game-ci/docker/tags
# $unityCiRepoVersion=0.17.0
# $os=ubuntu

# docker build --progress=plain `
#     --build-arg hubImage=unityci/hub:$os-$unityCiRepoVersion `
#     --build-arg baseImage=unityci/base:$os-$unityCiRepoVersion `
#     --build-arg version=$version `
#     --build-arg changeSet=$changeSet `
#     --build-arg module="ios android" `
#     .