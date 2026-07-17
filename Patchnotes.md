# RelicWars patchnotes

## 4.–17. juli 2026

Sammenlignet med repoets state ved slutningen af 4. juli (`00ac4a5`). Noterne inkluderer også de ændringer, der stadig ligger ukommitteret i working tree pr. 17. juli.

### Nye features

- **Harbor og sømilitær:** Harbor er tilføjet som fuldt bygge- og rekrutteringsvindue med naval units, omkostninger, stats, population cost, kapacitet, kø, ETA og progress.
- **Unit unlock-research:** Research har fået en separat Unlocks-kategori med typed unit-unlocks, fire unlock-grene og en separat Subjugation-definition. Serveren validerer nu både building-level og completed research før rekruttering.
- **Combat Simulator:** Tilføjet read-only battle simulation via backend og Unity. Spilleren kan sammenligne attacker/defender units og se losses, survivors, revived units, luck, modifiers og transportkapacitet uden at ændre spilstate.
- **Administration:** Nyt read-only vindue med aktive troop movements og stationed supports, inklusive rute, fase, city, koordinater, ejer/alliance, troop manifest, countdowns og automatisk refresh efter resolving.
- **NPC-landsbyer og city sites:** Kystbaserede city sites genereres deterministisk og vises på world map. NPC-villages backfilles med stabile placeringer, initiale bygninger, ressourcer og units.
- **NPC-building automation:** NPC-byer under 2.500 points kan nu få ét autoritativt building-job ad gangen. NPC-køer og player-køer behandles separat.

### Gameplay- og backendforbedringer

- Inter-island deployment beregner nu transportkrav og kapacitetsmargin. Attack, support, travel estimate og combat simulation afviser utilstrækkelig transport før mutation.
- Deployment-responser indeholder nu origin/target-location med NPC-, player- og allianceoplysninger, så read-only oversigter kan vise meningsfulde entity-links.
- Recruitment-køer viser kun serverens aktive jobs, og rekrutteringsflowet har ensrettede costs, capacity, samlet tid, UTC-ETA, progress og READY-state på tværs af Barracks, Stable, Workshop og Harbor.
- City-readet eksponerer autoritative produktionsbidrag og et detaljeret population-breakdown, som bruges direkte i HUD-tooltips.
- Building workers er opdelt i fejlisolerede loops med separate batches, parallelle aggregates, cooldowns, rollback og ét afsluttende save per job.
- En idempotent datamigration reparerer byer med manglende exotic-resource-beholdninger, så NPC-building automation ikke gentager samme valideringsfejl.
- Legacy conversation-participantkolonner er fjernet gennem en ny EF migration, og controllerkvalitet samt API-kontrakter har fået ekstra tests.

### UI og præsentation

- Recruitment-vinduerne er samlet i et nyt command-layout med metrics, resourceikoner, kompakte køkort og bedre phone/tablet-layout.
- City HUD har fået Inventory- og Administration-entry points, forbedrede exotic-resource tooltips samt tydeligere city production/population-information.
- Attack og Support i city inspection viser nu mission, rute, transport, troop manifest, status og serverens travel estimate i et mere kompakt layout.
- Global window chrome, tooltips, auth-vinduer og world selection er standardiseret omkring det fælles theme med forbedret deferred loading og responsive safe areas.
- Telefonlayoutet har fået mere konsistente touch targets, gesture-scroll og central registrering af responsive UI roots.
- Reports, Town Hall, Messaging og world-map overlays er komprimeret og gjort mere ensartede visuelt.

### Foreløbige UI-skeletter

- **Dailies:** Hver spiller får nu 20 servervalgte objectives per UTC-døgn med autoritativ progress, completion, coming-soon-status og automatisk reset. Rewards og claim kommer senere.
- **Inventory og Starter Quest:** Synlige HUD-entry points/ikoner er tilføjet, men de har endnu ikke et komplet gameplay-flow.
- **Subjugation:** Research-definition og unlock-katalog findes, men der er endnu ikke et command-, endpoint- eller ownership-flow.
