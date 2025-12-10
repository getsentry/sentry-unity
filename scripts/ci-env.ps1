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
        return "6000.0.63f1"
    }
    "unity6000.1" {
        return "6000.1.17f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
