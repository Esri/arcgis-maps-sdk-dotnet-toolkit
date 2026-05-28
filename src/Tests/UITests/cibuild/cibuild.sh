#!/usr/bin/env bash

test_platform=$1
if [ -z "${test_platform}" ]; then
  echo "Error: No test platform argument was provided to cibuild.sh, the build will fail." 1>&2
fi

function main {
  if [ -z "${WORKSPACE}" ]; then
    echo "WORKSPACE not set. Aborting dotnet install." 1>&2
    exit 1
  fi

  script_dir="$( realpath "$(dirname "${BASH_SOURCE[0]}")" )"

  yaml_config="${script_dir}/variables.yml"
  dotnet_version=$(read_yaml_var "${yaml_config}" "dotnet-version")

  install_dotnet "${WORKSPACE}" "${dotnet_version}" "${DOTNET_CACHE_FOLDER}"

  export YAML_CONFIG="${yaml_config}"
  "${DOTNET_EXE}" run "${script_dir}/utils.cs" -- $1
}

function install_dotnet {
  workspace=$1
  dotnet_version=$2
  dotnet_cache_dir=$3

  # Install the desired dotnet version if not already cached
  if [ -z "${dotnet_cache_dir}" ]; then
    dotnet_install_dir="${workspace}/.dotnet"
  else
    dotnet_install_dir="${dotnet_cache_dir}/${dotnet_version}"
  fi

  if [ ! -x "${dotnet_install_dir}/dotnet" ]; then
    mkdir -p "${dotnet_install_dir}"
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${workspace}/dotnet-install.sh"
    bash "${workspace}/dotnet-install.sh" --version "${dotnet_version}" --install-dir "${dotnet_install_dir}"
  fi

  export DOTNET_ROOT="${dotnet_install_dir}"
  export DOTNET_EXE="${dotnet_install_dir}"/dotnet
}

function read_yaml_var {
  yml_file=$1
  varname=$2
  grep "^${varname}" "${yml_file}" | sed -E "s/^${varname}: \"(.*)\"/\1/"
}

main "${test_platform}"
