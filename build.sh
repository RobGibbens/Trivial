#!/bin/bash

mono --runtime=v4.0 tools/NuGet/nuget.exe install FAKE -Version 4.1.0
mono --runtime=v4.0 packages/FAKE.4.1.0/tools/FAKE.exe build.fsx $@