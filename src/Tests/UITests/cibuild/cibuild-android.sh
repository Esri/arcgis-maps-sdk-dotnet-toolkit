#!/usr/bin/env bash

if [ -z "${WORKSPACE}" ]; then
  echo "WORKSPACE not set. Aborting dotnet install." 1>&2
  exit 1
fi

script_dir="$( realpath "$(dirname "${BASH_SOURCE[0]}")" )"
source "${script_dir}/utils.sh"

yaml_config="${script_dir}/variables.yml"
dotnet_version=$(read_yaml_var "${yaml_config}" "dotnet-version")

install_dotnet "${WORKSPACE}" "${dotnet_version}" "${DOTNET_CACHE_FOLDER}"

export YAML_CONFIG="${yaml_config}"
"${DOTNET_EXE}" run "${script_dir}/utils.cs"