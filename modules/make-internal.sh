#!/bin/bash
set -e

find Ben.Demystifier/src -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public static class/internal static class/g'
find Ben.Demystifier/src -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public partial class/internal partial class/g'
find Ben.Demystifier/src -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public class/internal class/g'
find Ben.Demystifier/src -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public struct/internal struct/g'
