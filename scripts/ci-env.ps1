param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2019" {
        return "2019.4.39f1"
    }
    "unity2020" {
        return "2020.3.37f1"
    }
    "unity2021" {
        return "2021.3.8f1"
    }
    "unity2022" {
        return "2022.1.12f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
