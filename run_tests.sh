#!/bin/bash
dotnet build 6IA-IT-Portal.slnx
dotnet test tests/TopekaIT.Core.Tests
