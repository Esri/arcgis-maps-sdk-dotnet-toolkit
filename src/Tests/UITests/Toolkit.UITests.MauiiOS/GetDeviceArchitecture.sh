#!/bin/sh

udid=$1

architecture="unknown"
if [ -n "$(xcrun simctl list devices | grep "$udid")" ]; then
  architecture="iossimulator-arm64"
elif [ -n "$(devicectl list devices --columns udid | grep "$udid")" ]; then
  architecture="ios-arm64"
else
  echo "Could not determine device architecture for udid: $udid"
  exit 1
fi

echo $architecture