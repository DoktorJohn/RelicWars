# Changelog

Lokal, Git-ignoreret arbejdslog.

| Filnavn | Ændring resume | Dato |
|---|---|---|
| `WorldPlayerService.cs` | Gjorde world join transaktionelt og genbrugte eksisterende participation efter samtidige uniqueness-konflikter. | 2026-07-18 |
| `GameContext.cs` | Håndhævede unikke profile-world-, city-koordinat- og typed map-object-koordinat-invarianter. | 2026-07-18 |
| `20260717223833_PreventDuplicateWorldParticipationAndCityCoordinates.cs` | Tilføjede constraints mod dublerede participations, byfelter og typed mapobjekter. | 2026-07-18 |
| `WorldPlayerServiceTests.cs` | Regressionstestede genbrug af vindende participation efter en samtidig databasekonflikt. | 2026-07-18 |
| `WorldMapRenderer.cs` | Bevarede terrain-rendering ved legacy-koordinatdubletter gennem deterministisk city-valg. | 2026-07-18 |
| `WorldSelectionWindowController.cs` | Blokerede gentagne world-join klik under aktive requests. | 2026-07-18 |
