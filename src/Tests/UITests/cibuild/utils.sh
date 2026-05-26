#!/usr/bin/env bash

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