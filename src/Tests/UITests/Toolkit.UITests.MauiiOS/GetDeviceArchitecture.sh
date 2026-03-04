#!/bin/sh

udid=$1

if [ -z "$udid" ]; then
  echo "No UDID provided.">&2
  exit 1
fi

architecture="unknown"

# Run commands to find the devices based on udid
xcrunResult=$(xcrun simctl list devices | grep -e "($udid)")
devicectlResult=$(devicectl list devices --columns udid | grep -e "\s$udid")

# Determine which command provided a result
if [ -n "$xcrunResult" ] && [ -n "$devicectlResult" ]; then
  echo "Both simulators and devices matched the given UDID: $udid">&2
  exit 1
elif [ -n "$xcrunResult" ]; then
  result=$xcrunResult
elif [ -n "$devicectlResult" ]; then
  result=$devicectlResult
else
  echo "The given UDID was not recognized as either a simulator or a physical device: $udid">&2
  exit 1
fi

# Check if multiple devices are found with the same UDID
if (( $(grep -c . <<<"$result") > 1 )); then
  echo "Multiple devices found with UDID: $udid">&2
  exit 1
fi

# Determine architecture based on which command provided a valid result
if [ -n "$xcrunResult" ]; then
  architecture="iossimulator-arm64"
elif [ -n "$devicectlResult" ]; then
  architecture="ios-arm64"
else
  echo "Could not determine device architecture for UDID: $udid">&2
  exit 1
fi

echo $architecture