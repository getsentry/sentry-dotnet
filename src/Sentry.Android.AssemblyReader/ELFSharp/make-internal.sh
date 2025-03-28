#!/bin/bash
set -e

find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public sealed class/internal sealed class/g'
find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public static class/internal static class/g'
find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public partial class/internal partial class/g'
find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public class/internal class/g'
find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public struct/internal struct/g'
find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public enum/internal enum/g'
find . -name \*.cs -print0 | xargs -0 sed -E -i '' 's/public interface/internal interface/g'
