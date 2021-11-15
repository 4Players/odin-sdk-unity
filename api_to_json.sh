#!/bin/bash

docfx

for filename in documentation/*.yml; do
    yq eval -o=j "$filename" > documentation/$(basename "$filename" .yml).json
done

rm documentation/.manifest
rm documentation/*.yml
