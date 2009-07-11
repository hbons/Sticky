#!/bin/bash
gmcs main.cs -pkg:gtk-sharp-2.0 -r:System.Data.dll -r:Mono.Data.SqliteClient.dll
mono main.exe 
