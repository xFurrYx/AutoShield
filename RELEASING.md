# Releasing a new version

Hexium accepts each version number once, so test before uploading.

1. Bump the version in all three places (`package.py` refuses to build if they differ):
   - `package/manifest.json` → `version_number`
   - `src/Plugin.cs` → `Version`
   - `src/AutoShield.csproj` → `<Version>`
2. Add a section at the top of `package/CHANGELOG.md`.
3. If you edited `README.md`, copy it to `package/README.md` (they must match).
4. `./package.py --profile "<Gale profile folder>"`, import `dist/AutoShield-X.Y.Z.zip` into Gale, and test in game.
5. Commit, tag and push:
   ```sh
   git commit -am "Release X.Y.Z"
   git tag vX.Y.Z
   git push && git push --tags
   ```
6. Upload the same zip on hexium.gg under Blade_of_Furry (install target: client).
