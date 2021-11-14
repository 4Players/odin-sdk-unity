#!/bin/bash
for filename in documentation/*.yml; do
    yq eval -o=j "$filename" > odin_unity_api/$(basename "$filename" .yml).json
done