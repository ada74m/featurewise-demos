#!/usr/bin/env bash
# Builds a package for each demo service, and one for the demo sign-in, all at one version, as
# <out-dir>/<package>-<version>.zip, with the same contents unpacked in <out-dir>/<package>. Each package
# keeps the repository's layout, so its modules find Common's by the same relative paths, and carries the
# zip of each function it deploys in functions/. An environment uses one as
# source = "s3::https://.../demos/<package>/<version>.zip//<Folder>/Infrastructure".
set -euo pipefail
version=${1:?usage: package.sh <version> <out-dir>}
mkdir -p "${2:?usage: package.sh <version> <out-dir>}"
out=$(cd "$2" && pwd)
cd "$(dirname "$0")"

rm -rf "$out/publish"

# Publishes a project for Lambda, as <out-dir>/publish/<project>.zip.
publish() {
  local project=$1 name
  name=$(basename "$project")
  dotnet publish "$project" -c Release -r linux-arm64 --self-contained false \
    -o "$out/publish/$name" --nologo -v quiet
  (cd "$out/publish/$name" && zip -qrX "$out/publish/$name.zip" .)
}

# Copies a folder's Terraform, keeping its place in the repository.
terraform_of() {
  local package=$1 folder=$2
  mkdir -p "$package/$folder"
  rsync -a --include='*/' --include='*.tf' --exclude='*' --prune-empty-dirs "$folder/" "$package/$folder/"
}

# Zips a package's folder as <out-dir>/<name>-<version>.zip.
finish() {
  local name=$1
  echo "$version" > "$out/$name/VERSION"
  rm -f "$out/$name-$version.zip"
  (cd "$out/$name" && zip -qrX "$out/$name-$version.zip" .)
  echo "$out/$name-$version.zip"
}

# A demo: its functions, its module for the UI shell, its Terraform with Common's, and its description.
demo() {
  local name=$1 folder=$2 project=$3
  local package="$out/$name"
  rm -rf "$package"
  mkdir -p "$package/functions"

  publish "$folder/$project.Functions"
  cp "$out/publish/$project.Functions.zip" "$package/functions/"

  (cd "$folder/$project.Ui" && npm ci --silent && npm run --silent build)
  mkdir -p "$package/$folder/$project.Ui/dist"
  rsync -a "$folder/$project.Ui/dist/" "$package/$folder/$project.Ui/dist/"

  terraform_of "$package" Common/Infrastructure
  terraform_of "$package" "$folder/Infrastructure"
  cp "$folder/demo.json" "$package/$folder/"
  finish "$name"
}

demo lost-property LostProperty Demos.LostProperty
demo box-office BoxOffice Demos.BoxOffice

# The demo sign-in: its function and its Terraform, with the Lambda module it uses from Common.
package="$out/sign-in"
rm -rf "$package"
mkdir -p "$package/functions"
publish SignIn/Demos.SignIn
cp "$out/publish/Demos.SignIn.zip" "$package/functions/"
terraform_of "$package" Common/Infrastructure/modules/lambda-function
terraform_of "$package" SignIn/Infrastructure
finish sign-in
