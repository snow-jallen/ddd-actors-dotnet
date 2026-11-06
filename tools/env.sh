# Source this to use the locally-installed .NET 11 SDK for this book:
#   source tools/env.sh
_BOOK_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_ROOT="$_BOOK_ROOT/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
# Android SDK + JDK installed locally for building the .NET MAUI head (net11.0-android)
if [ -d "$_BOOK_ROOT/.android-sdk" ]; then
  export ANDROID_HOME="$_BOOK_ROOT/.android-sdk"
  export AndroidSdkDirectory="$ANDROID_HOME"
fi
if [ -d "$_BOOK_ROOT/.jdk" ]; then
  export JAVA_HOME="$_BOOK_ROOT/.jdk"
  export JavaSdkDirectory="$JAVA_HOME"
fi
