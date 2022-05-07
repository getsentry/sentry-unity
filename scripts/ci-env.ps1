param (
    [Parameter()]
    [string] $name
)

switch ($name) {
    "unity2019" {
        return "2019.4.38f1"
    }
    "unity2020" {
        return "2020.3.34f1"
    }
    "unity2021" {
        return "2021.3.2f1"
    }
    Default {
        throw "Unkown variable '$name'"
    }
}
