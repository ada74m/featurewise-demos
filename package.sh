#!/usr/bin/env bash
# Builds the demos service's package for a version: its Terraform module and the zip of each function it
# deploys, as <out-dir>/demos-<version>.zip, with the same contents unpacked in <out-dir>/demos. An
# environment uses it as source = "s3::https://.../demos/<version>.zip//Infrastructure".
set -euo pipefail
version=${1:?usage: package.sh <version> <out-dir>}
mkdir -p "${2:?usage: package.sh <version> <out-dir>}"
out=$(cd "$2" && pwd)
cd "$(dirname "$0")"

package="$out/demos"
rm -rf "$package" "$out/publish" "$out/demos-$version.zip"
mkdir -p "$package/functions"

# The module for the UI shell, which the package uploads to the shell's bucket.
(cd Demos.Ui && npm ci --silent && npm run --silent build)
mkdir -p "$package/Demos.Ui/dist"
rsync -a Demos.Ui/dist/ "$package/Demos.Ui/dist/"

for project in UiBackend ScheduledWorker QueueWorker SignIn; do
  dotnet publish "Demos.$project" -c Release -r linux-arm64 --self-contained false \
    -o "$out/publish/Demos.$project" --nologo -v quiet
  (cd "$out/publish/Demos.$project" && zip -qrX "$package/functions/Demos.$project.zip" .)
done

# The modules keep their places relative to each other, as their sources refer to each other by path.
for module in Infrastructure Common/Infrastructure Demos.SignIn/Infrastructure; do
  mkdir -p "$package/$module"
  rsync -a --include='*/' --include='*.tf' --exclude='*' --prune-empty-dirs "$module/" "$package/$module/"
done

# The demos, with their personas, which the module's demos output reads.
cp demos.json "$package/"

echo "$version" > "$package/VERSION"
(cd "$package" && zip -qrX "$out/demos-$version.zip" .)
echo "$out/demos-$version.zip"
