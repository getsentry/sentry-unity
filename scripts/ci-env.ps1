param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2020.3" {
        return "2020.3.48f1"
    }
    "unity2021.3" {
        return "2021.3.45f1"
    }
    "unity2022.3" {
        return "2022.3.62f1"
    }
    "unity2023" {
        return "2023.2.20f1"
    }
    "unity6000.0" {
        return "6000.0.54f1"
    }
    "unity6000.1" {
        return "6000.1.12f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
