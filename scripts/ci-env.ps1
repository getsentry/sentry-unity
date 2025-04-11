param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2019" {
        return "2019.4.40f1"
    }
    "unity2020" {
        return "2020.3.48f1"
    }
    "unity2021" {
        return "2021.3.45f1"
    }
    "unity2022" {
        return "2022.3.60f1"
    }
    "unity2023" {
        return "2023.2.20f1"
    }
    "unity6000" {
        return "6000.0.45f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
