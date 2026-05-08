param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2021.3"           { return "2021.3.45f2" }
    "unity2021.3-changeset" { return "88f88f591b2e" }
    "unity2022.3"           { return "2022.3.62f3" }
    "unity2022.3-changeset" { return "96770f904ca7" }
    "unity6000.0"           { return "6000.0.74f1" }
    "unity6000.0-changeset" { return "7685f01dc6be" }
    "unity6000.3"           { return "6000.3.14f1" }
    "unity6000.3-changeset" { return "d68c3f99a318" }
    Default                 { throw "Unkown variable '$name'" }
}
