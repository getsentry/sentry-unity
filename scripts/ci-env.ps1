param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2019" {
        return "2019.4.40f1"
    }
    "unity2020" {
        return "2020.3.46f1"
    }
    "unity2021" {
        return "2021.3.22f1"
    }
    "unity2022" {
        return "2022.2.13f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
