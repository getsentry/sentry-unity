param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2019" {
        return "2019.4.39f1"
    }
    "unity2020" {
        return "2020.3.36f1"
    }
    "unity2021" {
        return "2021.3.4f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
