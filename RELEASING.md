# Releasing a new version

Hexium accepts each version number once, so test before uploading.

1. Bump the version in all three places, keeping them identical:
   - `manifest.json` → `version_number`
   - `AutoShield/Plugin.cs` → `Version`
   - `AutoShield/AutoShield.csproj` → `<Version>`
2. Add a section at the top of `CHANGELOG.md`.
3. `dotnet build -c Release` and test `dist/AutoShield-X.Y.Z.zip` in game.
4. Commit, tag and push:
   ```sh
   git commit -am "Release X.Y.Z"
   git tag vX.Y.Z
   git push && git push --tags
   ```
5. `gh release create vX.Y.Z dist/AutoShield-X.Y.Z.zip --title "X.Y.Z" --notes-file <(sed -n '/^## X.Y.Z/,/^## /p' CHANGELOG.md | sed '$d')`
6. Upload the same zip on hexium.gg (install target: client).

## Package layout (Hexium)

```
AutoShield-X.Y.Z.zip
├── manifest.json      required, zip root
├── icon.png           required, exactly 256×256
├── README.md          required, becomes the mod page
├── CHANGELOG.md       optional, shown on the versions page
├── LICENSE.md
└── plugins/
    └── AutoShield.dll
```
