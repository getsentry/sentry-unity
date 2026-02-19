param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2021.3" {
        return "2021.3.45f2"
    }
    "unity2022.3" {
        return "2022.3.62f3"
    }
    "unity6000.0" {
        return "6000.0.67f1"
    }
    "unity6000.3" {
        return "6000.3.9f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
