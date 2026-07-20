# Changelog

Lokal, Git-ignoreret arbejdslog.

| Filnavn | Ændring resume | Dato |
|---|---|---|
| `DailyObjectiveService.cs` | Begrænser standalone retry til konkrete daily-entries og dækker både progress, read og rollover. | 2026-07-20 |
| `DailyObjectiveServiceTests.cs` | Verificerer daily-only retry for standalone progress og read samt propagation af andre concurrency-fejl. | 2026-07-20 |
| `WorldPlayerRepositoryTests.cs` | LocalDB-regressionstester stale daily-rowversions under economy-save og bevarer nyere assignment-progress. | 2026-07-20 |
| `20260719182549_RepairResearchLegacyUserIdColumn.cs` | Fjerner idempotent den umappede obligatoriske Researches.UserId-kolonne, som blokerede workerens research-completion inserts. | 2026-07-19 |
| `20260719182549_RepairResearchLegacyUserIdColumn.Designer.cs` | Registrerer EF-targetmodellen for migrationen, der reparerer den efterladte legacy-kolonne på Researches. | 2026-07-19 |
| `ResearchLegacyColumnMigrationTests.cs` | LocalDB-regressionstester databevarende migration, rollback-idempotens og efterfølgende research-completion insert gennem EF-modellen. | 2026-07-19 |
| `ExoticResourceMigrationTests.cs` | Opdaterer forventet seneste migration efter tilføjelsen af Researches legacy-kolonnereparationen i migrationsregressionen. | 2026-07-19 |
| `ExoticResourceService.cs` | Udvider valideringsfejlen med aktuelt rækketal og konkrete manglende exotic resource-typer uden automatisk reparation. | 2026-07-19 |
| `ExoticResourceServiceTests.cs` | Verificerer den diagnostiske exotic-resource-fejls rækketal og komplette liste over manglende typer. | 2026-07-19 |
| `NPCBuildingWorkerTests.cs` | Regressionstester at en ugyldig NPC-by ikke forhindrer en efterfølgende gyldig by i køopfyldning. | 2026-07-19 |
| `ExoticResourceMigrationTests.cs` | LocalDB-regressionstester idempotent repair-migration, bevarede saldi, ti unikke typer og det virkelige NPC-repository-load. | 2026-07-19 |
| `DailyObjectiveService.cs` | Genindlæser daily-state én gang efter standalone row-version-konflikt og retries eventet i en ny låst transaktion. | 2026-07-19 |
| `IDailyObjectiveRepository.cs` | Tilføjer afgrænset reset af en spillers trackede daily-objective-state efter rollback. | 2026-07-19 |
| `DailyObjectiveRepository.cs` | Detacher kun den konfliktramte spillers daily set og assignments før kontrolleret genindlæsning. | 2026-07-19 |
| `DailyObjectiveServiceTests.cs` | Regressionstester enkelt retry, tracking-reset og succes efter en simuleret daily concurrency-konflikt. | 2026-07-19 |
| `WorldPlayerRepository.cs` | Gemmer trackede WorldPlayer-ændringer uden rekursivt at markere daily-objective-grafen og stale row-versioner som modificerede. | 2026-07-19 |
| `WorldPlayerRepositoryTests.cs` | Regressionstester at economy-save kun markerer WorldPlayer og ikke den relationship-fixede daily-objective-graf. | 2026-07-19 |
| `WorldMapRenderer.cs` | Initialiserer kortet fra matchende active-city state, requester første chunks deterministisk og recentrerer efter autoritative bounds. | 2026-07-18 |
| `CameraEdgePan.cs` | Fjerner ugyldige positive fallback-bounds og låser input samt edge-pan, indtil kortets initialfokus er stabilt. | 2026-07-18 |
| `CityStateManager.cs` | Eksponerer eksplicit city-id-bundet readiness og rydder korrekt navngivne current-city-koordinater ved byskift. | 2026-07-18 |
| `NetworkManager.cs` | Centraliserer sessionens aktive city og udsender et event, når spilleren reelt vælger en anden by. | 2026-07-18 |
| `CityTopBarViewController.CitySelector.cs` | Lader dropdown og pile skifte ActiveCityId, city-poller og label gennem samme selector-flow. | 2026-07-18 |
| `WorldHexagonWindowController.cs` | Bruger de korrekt navngivne current-city-koordinater som origin i combat simulator-payload. | 2026-07-18 |
| `WorldMapScene.unity` | Fjerner scene-serialiserede fallback-grænser, som fejlagtigt clampede negative world-koordinater før første chunkrespons. | 2026-07-18 |
| `ResearchService.cs` | Håndhæver centralt University-kravet i research-tree og start-command før pointfradrag eller joboprettelse. | 2026-07-18 |
| `Backend/Application/DTOs/ResearchDTOs.cs` | Udvider backendens research-tree med global startmulighed og autoritative manglende krav. | 2026-07-18 |
| `ResearchData.cs` | Retter misvisende legacy-kommentar, så research points beskrives som WorldPlayer-global betaling. | 2026-07-18 |
| `ResearchServiceTests.cs` | Regressionstester afvisning uden University, bevarede points, tree-requirement og University i en anden ejet by. | 2026-07-18 |
| `WorldPlayerRepositoryTests.cs` | Verificerer at world-player-loadet indeholder University-buildings fra alle spillerens byer til research-validering. | 2026-07-18 |
| `Unity/Assets/_Project/Scripts/Domain/DTOs/ResearchDTOs.cs` | Spejler backendens additive research-tree-kontrakt med global startmulighed og autoritative manglende krav i Unity. | 2026-07-18 |
| `ResearchWindowController.cs` | Binder serverens research-availability, viser command-fejl og holder University-gaten i vinduets state. | 2026-07-18 |
| `ResearchWindowController.Rendering.cs` | Viser autoritative research-krav og deaktiverer startknapper, mens spilleren mangler University. | 2026-07-18 |
| `ResearchWindow.uxml` | Tilføjer et tomt, dynamisk requirement-banner mellem research points og research-træet. | 2026-07-18 |
| `ResearchWindow.uss` | Styler University-requirement som et responsivt warning-banner med global semantisk farve. | 2026-07-18 |
| `NetworkManager.cs` | Persisterer valgfri JWT-session, validerer tokenets levetid ved opstart og rydder remembered login ved logout. | 2026-07-18 |
| `BootstrapLoader.cs` | Springer login-scenen over og åbner world selection, når NetworkManager har genoprettet en gyldig session. | 2026-07-18 |
| `LoginWindowController.cs` | Binder Remember me-valget til loginflowet og deaktiverer togglen sammen med øvrige controls under request. | 2026-07-18 |
| `LoginWindow.uxml` | Tilføjer en Remember me-toggle mellem passwordfeltet og loginstatus i den eksisterende auth-formular. | 2026-07-18 |
| `AuthWindow.uss` | Styler Remember me-togglen med auth-tema og sikrer mindst 44 pixels touch target på telefon. | 2026-07-18 |
| `CityStatService.cs` | Medregner garnison, alle origin-deployments og resterende recruitment i byens autoritative populationforbrug. | 2026-07-18 |
| `CityService.cs` | Genbruger central unit-population til coin-upkeep og fjerner den overflødige unit-reader dependency. | 2026-07-18 |
| `ResourceService.cs` | Beregner unit-upkeep gennem CityStatService og fjerner duplikeret stack-summering samt unit-reader dependency. | 2026-07-18 |
| `CityRepository.cs` | Indlæser origin-deployments med stacks for alle player cities under job- og økonomiprocessering. | 2026-07-18 |
| `CityStatServiceTests.cs` | Regressionstester deploymentfaser, target-eksklusion, recruitment-addition, tab og populationsneutral hjemkomst. | 2026-07-18 |
| `CityRepositoryTests.cs` | Verificerer job-queryens indlæsning af origin-deployment stacks på tværs af spillerens byer. | 2026-07-18 |
| `RecruitmentServiceTests.cs` | Bekræfter at udsendte units fortsat reserverer population og forhindrer kapacitetsoverskridende recruitment. | 2026-07-18 |
| `ResourceServiceProductionTests.cs` | Verificerer uændret modificeret unit-upkeep for både garnisonerede og udsendte units. | 2026-07-18 |
| `CityServiceTownHallTests.cs` | Tilpasser alle CityService-konstruktionstests til den smallere dependency-kontrakt uden den tidligere unit-reader. | 2026-07-18 |
| `ExoticResourceServiceTests.cs` | Tilpasser ResourceService-fixturen til centraliseret populationberegning og fjernet unit-reader parameter. | 2026-07-18 |
| `IdeologyFocusGameplayTests.cs` | Tilpasser economy-focus-fixturen til ResourceServices smallere constructor efter populationcentraliseringen. | 2026-07-18 |
| `TestSupport.cs` | Lader fælles city-stat-testdouble medregne garnison og deployments i populationforbruget. | 2026-07-18 |
| `azure-static-web-apps-yellow-mud-024ff6303.yml` | Genererer deployment-proveniens med commit, UTC-tid og SHA-256 for alle fire WebGL-buildfiler. | 2026-07-18 |
| `staticwebapp.config.json` | Kræver cache-revalidation for index, provenance, loader og WebGL-filer uden at miste Brotli-headere. | 2026-07-18 |
| `WorldPlayerService.cs` | Gjorde world join transaktionelt og genindlæser vindende participation efter samtidige databasekonflikter. | 2026-07-18 |
| `WorldService.cs` | Normaliserer legacy-citydubletter i chunkrespons deterministisk og logger valgte samt fravalgte ids. | 2026-07-18 |
| `GameContext.cs` | Håndhævede unikke profile-world-, world-city- og typed map-object-koordinater. | 2026-07-18 |
| `20260718135143_PreventDuplicateWorldParticipationAndCityCoordinates.cs` | Afviser resterende dubletgrupper før oprettelse af tre unikke produktionsindeks. | 2026-07-18 |
| `20260718135143_PreventDuplicateWorldParticipationAndCityCoordinates.Designer.cs` | Registrerede den genererede targetmodel for world-join uniqueness-migrationen. | 2026-07-18 |
| `GameContextModelSnapshot.cs` | Synkroniserede EF-modellen med de tre nye world-scopede uniqueness-invarianter. | 2026-07-18 |
| `WorldPlayerServiceTests.cs` | Regressionstestede dobbelt-join, rollback, vinder-genindlæsning og databaseafvisning af alle tre dublettyper. | 2026-07-18 |
| `WorldServiceTests.cs` | Verificerede at chunkrespons vælger laveste city-id ved dublerede koordinater. | 2026-07-18 |
| `WorldMapRenderer.cs` | Bevarede terrain ved dubletter og tilføjede development-logs for city samt første centerchunk. | 2026-07-18 |
| `WorldSelectionWindowController.cs` | Deaktiverede alle Enter-knapper under join og genaktiverede dem efter fejl. | 2026-07-18 |
| `DailyObjectiveService.cs` | Erstattede proceslokale spillerlåse med transaction-owned repositorylås før daily reads, rollover og progression. | 2026-07-18 |
| `IDailyObjectiveRepository.cs` | Udvidede daily repository-kontrakten med en spillerafgrænset application-lock operation. | 2026-07-18 |
| `DailyObjectiveRepository.cs` | Implementerede transaction-owned SQL Server application lock med femten sekunders timeout og concurrency-fejl. | 2026-07-18 |
| `WorldPlayerController.cs` | Lod economy-endpointets uventede exceptions nå global middleware, inklusive concurrency-konflikter. | 2026-07-18 |
| `DailyObjectiveServiceTests.cs` | Regressionstestede commit-serialisering, samtidig rollover og progression, rollback-frigivelse samt separate spillerressourcer. | 2026-07-18 |
| `ControllerQualityTests.cs` | Verificerede at economy-endpointets concurrency-fejl ikke konverteres til generisk serverfejl. | 2026-07-18 |
Backend/Application/Services/DailyObjectiveService.cs | Detacher allerede tracket daily-state under spillerlåsen før genindlæsning og opdatering. | 2026-07-20
Backend/Application.Tests/DailyObjectiveServiceTests.cs | Verificerer lock-detach-load-rækkefølge og tilpasser retry-forventninger til sikker genindlæsning. | 2026-07-20
