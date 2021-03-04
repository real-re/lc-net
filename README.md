# LC-net

> [LC](https://github.com/real-re/lc) implement by net 5.0

![preview]()

## Overview

## Guide

``` bash
git clone https://github.com/real-re/lc-net
cd lc-net

dotnet run "

name = Simple Animation Config

[Idle]
frames = [
    idle_01
    idle_02
    idle_03
]"

/*
---------------------
Parsing LC...
---------------------

KEY   : name
VALUE : Simple
SECTION: Idle
KEY   : frames
[ --> Begin Array
        VALUE-ONLY: idle_01
        VALUE-ONLY: idle_02
        VALUE-ONLY: idle_03
] --> End Array

---------------------
Print result...
---------------------

[Section] : __MAIN__
name : Simple


[Section] : Idle
frames : [
idle_01
idle_02
idle_03
]
*/
```
