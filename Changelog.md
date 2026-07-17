# Changelog

Denne fil registrerer kodeændringer udført i hvert prompt. Tilføj én række per ændret kodefil, placér nyeste poster øverst, skriv et resume på 10-25 ord, og brug datoformatet `YYYY-MM-DD`.

Dokumentationsændringer og genererede filer registreres ikke som kodeændringer.

| Filnavn | Ændring resume 10-25 ord | Dato |
|---|---|---|
| `20260717195246_RepairCityExoticResourceBalances.cs` | Reparerede idempotent manglende exotic-resource-beholdninger for eksisterende byer, som blev oprettet efter den oprindelige backfill. | 2026-07-17 |
| `LoginWindow.uxml` | Tilføjede brun standardheader, så login-scenen matcher City Overviews tydelige window chrome. | 2026-07-17 |
| `RegisterWindow.uxml` | Tilføjede brun standardheader, så registreringsscenen matcher City Overviews tydelige window chrome. | 2026-07-17 |
| `AuthWindow.uss` | Centrerede auth-sceners nye standardheadere og fjernede vinduesdragmarkøren fra de faste paneler. | 2026-07-17 |
| `WorldSelectionWindow.uxml` | Genindførte en brun standardheader på world selection med den delte firefarvede window-frame. | 2026-07-17 |
| `WorldSelectionWindow.uss` | Centrerede world selections nye header og markerede den faste sceneoverflade som ikke-flytbar. | 2026-07-17 |
| `DailiesWindow.uxml` | Begrænsede objective-tabellen til vertikal scrolling og skjulte den horisontale scrollbar eksplicit. | 2026-07-17 |
| `DailiesWindow.uss` | Gjorde alle daily-kolonner proportionelt fleksible, så tabellen altid tilpasses den tilgængelige bredde. | 2026-07-17 |
| `BaseWindow.cs` | Omdøbte deferred-open-tælleren, så Unity-serialisering ikke kolliderer med Administration- og Dailies-felter. | 2026-07-17 |
| `ITransactionManager.cs` | Eksponerede aktiv transaktion, så daily-progress kan stage ændringer til workerens afsluttende save. | 2026-07-17 |
| `TransactionManager.cs` | Implementerede aktiv transaktionsstatus direkte fra den aktuelle delte EF Core context. | 2026-07-17 |
| `DailyObjectivesControllerTests.cs` | Verificerede endpointets autoritative DTO-shape, world-player-id forwarding og fravær af claim-action. | 2026-07-17 |
| `DailyObjectiveServiceTests.cs` | Testede katalog, selection, tiergrænser, reset, låste objectives, produktion og samtidige progress-events. | 2026-07-17 |
| `DailyObjectiveDTO.cs` | Definerede autoritativ daily-respons med UTC-vindue, tyve slots, reward-tier, progress og state. | 2026-07-17 |
| `IDailyObjectiveRepository.cs` | Tilføjede persistence-kontrakt til indlæsning og atomisk erstatning af spillerens aktuelle daily-set. | 2026-07-17 |
| `IDailyObjectiveService.cs` | Definerede read-, gameplay-progress- og produktionskontrakter for autoritative spiller-specifikke daily objectives. | 2026-07-17 |
| `DailyObjectiveService.cs` | Implementerede lazy UTC-reset, 10+10-selection, vægtet reroll, clamped progress og coming-soon-beskyttelse. | 2026-07-17 |
| `IWorldPlayerService.cs` | Udvidede global resource-sync med asynkron variant til atomisk persistent daily-produktionsprogress. | 2026-07-17 |
| `WorldPlayerService.cs` | Krediterede positiv netto-Coin-produktion med præcist UTC-midnatsklip før autoritativ global resource-sync. | 2026-07-17 |
| `CityService.cs` | Brugte asynkron global resource-sync, så city reads også krediterer dagens Coin-objectives. | 2026-07-17 |
| `ExoticResourceService.cs` | Krediterede faktisk samlet exotic-produktion per city og klippede produktionsintervallet ved UTC-midnat. | 2026-07-17 |
| `IdeologyFocusService.cs` | Krediterede succesfulde Focuses og instant-granted units samt synkroniserede Coins før Focus-fradrag. | 2026-07-17 |
| `JobService.cs` | Krediterede færdige buildings, city points, Housing-kapacitet, leverede units og afsluttet research. | 2026-07-17 |
| `RecruitmentService.cs` | Brugte asynkron resource-sync før recruitment, så positiv Coin-produktion registreres samme gameplay-flow. | 2026-07-17 |
| `UnitDeploymentWorker.cs` | Krediterede permanente kampkills, naval kills, sejre, successful attacks, defenses og support-attribution. | 2026-07-17 |
| `DailyObjectiveAssignment.cs` | Tilføjede persistente slot-assignments med definition, target, decimalprogress, completion og concurrency-token. | 2026-07-17 |
| `DailyObjectiveSet.cs` | Tilføjede spillerens aktuelle UTC-daterede daily-set med assignments og row-version concurrency-token. | 2026-07-17 |
| `DailyObjectiveEnums.cs` | Definerede stabile reward-tiers, progress-typer og API-states for det nye daily objective-domæne. | 2026-07-17 |
| `DailyObjectiveData.cs` | Modellerede definitionskatalog, selection-regler, unit-filtre, targets og eksplicit implementeringsstatus fra JSON. | 2026-07-17 |
| `DailyObjectiveDataReader.cs` | Validerede ved startup 51 unikke definitioner, targets, tiers, vægte og nødvendige puljestørrelser. | 2026-07-17 |
| `WorldPlayer.cs` | Tilføjede entydig navigation til spillerens eneste aktuelle persistente daily objective-set. | 2026-07-17 |
| `DailyObjectivesController.cs` | Tilføjede autoriseret read-endpoint for spillerens aktuelle daily objectives uden claim-flow. | 2026-07-17 |
| `Program.cs` | Loadede daily-kataloget ved startup og registrerede repository samt application-service i dependency injection. | 2026-07-17 |
| `Game.csproj` | Gjorde det komplette daily objective-katalog til backendens aktive output-kopierede definitionsfil. | 2026-07-17 |
| `daily-objectives-complete.json` | Strukturerede 51 definitioner, rettede navne og tekster samt markerede implementerede og låste objectives. | 2026-07-17 |
| `GameContext.cs` | Mappede persistente daily-tabeller, one-to-one-ejerskab, unikke indexes, cascades og row-version concurrency. | 2026-07-17 |
| `DailyObjectiveRepository.cs` | Implementerede tracked indlæsning og staged atomisk udskiftning af daily-set gennem EF Core. | 2026-07-17 |
| `DailyObjectiveDTOs.cs` | Spejlede backendens komplette daily response, reward-tier og state-kontrakt direkte i Unity. | 2026-07-17 |
| `ClientDailyObjectivesService.cs` | Tilføjede autoriseret callback-baseret daily GET-klient med projektets fælles korrekte API-fejlfortolkning. | 2026-07-17 |
| `NetworkManager.cs` | Eksponerede den nye daily client service gennem den persistente netværkssingleton. | 2026-07-17 |
| `DailiesWindowController.cs` | Erstattede placeholders med deferred server-load, retry, autoritative rows og automatisk reset-refresh. | 2026-07-17 |
| `DailiesWindow.uxml` | Tilføjede eksplicit loading-state, fleksibel data-container og read-only STATUS-header uden claim-kontrol. | 2026-07-17 |
| `DailiesRow.uxml` | Erstattede den deaktiverede claim-knap med read-only statuslabel for hver objective-state. | 2026-07-17 |
| `DailiesWindow.uss` | Stylede fleksibel dataflade samt tydelige semantiske complete-, progress- og coming-soon-statusser. | 2026-07-17 |
| `LoginWindow.uxml` | Fjernede mørk topbar, brandtekster, beskrivelser og password-toggle fra det forenklede login-layout. | 2026-07-17 |
| `RegisterWindow.uxml` | Reducerede registreringsscenen til Create Account, formular, funktionel feedback og eksisterende navigation. | 2026-07-17 |
| `AuthWindow.uss` | Komprimerede den headerfri auth-shell og fjernede styling til brandtekster samt password-toggle. | 2026-07-17 |
| `LoginWindowController.cs` | Fjernede password-visibility state, callback og binding samt opdaterede loginformularens tab-rækkefølge. | 2026-07-17 |
| `RegisterWindowController.cs` | Fjernede password-togglelogik og bevarede registreringsformularens permanente maskering samt sammenhængende tab-rækkefølge. | 2026-07-17 |
| `WorldSelectionWindow.uxml` | Fjernede topbar og beskrivelser samt stablede større velkomst og spillernavn over realm-listen. | 2026-07-17 |
| `WorldSelectionWindow.uss` | Stylede selvstændig Back-kontrol, større todelt velkomst og et kortere headerfrit world-panel. | 2026-07-17 |
| `ResponsiveLayout.uss` | Fjernede forældede phone-regler til world selections tidligere vandrette velkomstrække. | 2026-07-17 |
| `LoginWindow.uxml` | Flyttede login til global window-frame, standardheader, content-container, game-inputs og fælles knapklasser uden controllerkontraktændring. | 2026-07-17 |
| `RegisterWindow.uxml` | Flyttede register til samme globale auth-shell med standardiserede input-, fejl- og knapklasser. | 2026-07-17 |
| `AuthWindow.uss` | Ensrettede login og register omkring globalt window chrome, safe area, responsive layout og lokale auth-formregler. | 2026-07-17 |
| `WindowTheme.uss` | Gjorde btn-text-link global, så auth-navigation deler theme-styling med øvrige tekstlinks. | 2026-07-17 |
| `WorldSelectionWindow.uxml` | Ombyggede world selection til global window-frame, standardheader, content-container, vignette og statiske USS-klasser. | 2026-07-17 |
| `WorldSelectionWindow.uss` | Stylede world selection med global palette, responsive safe area, standardheader og theme-baserede realm-kort. | 2026-07-17 |
| `WorldEntryItem.uxml` | Tilføjede realm-accent og global enter-knapklasse uden at ændre controllerens template-bindinger. | 2026-07-17 |
| `TownHallWindow.uss` | Fordelte de tolv sekundære bygningskort i et komplet firekolonnet grid under Town Hall. | 2026-07-17 |
| `BattleReportWindowController.cs` | Reducerede klikbare rapportposter til én linje med hændelsestitel og kompakt UTC-dato og klokkeslæt. | 2026-07-17 |
| `ReportsWindow.uss` | Komprimerede rapportlistens kort til lave énlinjerækker med ellipsis og fortsat visuel unread-markering. | 2026-07-17 |
| `BarracksWindow.uxml` | Fjernede unitbeskrivelsen fra Barracks og bevarede et særskilt felt til låsekrav. | 2026-07-17 |
| `StableWindow.uxml` | Fjernede unitbeskrivelsen fra Stable og bevarede et særskilt felt til låsekrav. | 2026-07-17 |
| `WorkshopWindow.uxml` | Fjernede unitbeskrivelsen fra Workshop og bevarede et særskilt felt til låsekrav. | 2026-07-17 |
| `HarborWindow.uxml` | Fjernede unitbeskrivelsen fra Harbor og bevarede et særskilt felt til låsekrav. | 2026-07-17 |
| `BarracksWindowController.cs` | Erstattede Barracks flavor-labelreferencen med et dedikeret felt til låste units kravtekst. | 2026-07-17 |
| `StableWindowController.cs` | Erstattede Stable flavor-labelreferencen med et dedikeret felt til låste units kravtekst. | 2026-07-17 |
| `WorkshopWindowController.cs` | Erstattede Workshop flavor-labelreferencen med et dedikeret felt til låste units kravtekst. | 2026-07-17 |
| `HarborWindowController.cs` | Fjernede naval flavortekst og viser nu kun kravfeltet, når den valgte unit er låst. | 2026-07-17 |
| `BarracksWindowController.Recruitment.cs` | Fjernede infantry flavortekst og viser nu kun kravfeltet, når den valgte unit er låst. | 2026-07-17 |
| `StableWindowController.Recruitment.cs` | Fjernede cavalry flavortekst og viser nu kun kravfeltet, når den valgte unit er låst. | 2026-07-17 |
| `WorkshopWindowController.Recruitment.cs` | Fjernede siege flavortekst og viser nu kun kravfeltet, når den valgte unit er låst. | 2026-07-17 |
| `HarborWindowController.cs` | Synkroniserede Harbor costs, timing, UTC-ETA og progressbaserede queue-cards med de øvrige recruitment-vinduer. | 2026-07-17 |
| `HarborWindow.uxml` | Ombyggede Harbor til det fælles recruitment-command-layout med paneler, metrics, resourceikoner, timing og queue. | 2026-07-17 |
| `TownHallWindowController.BuildingGrid.cs` | Sorterede Town Hall først og placerede de øvrige tolv bygninger i et separat trekolonnet undergrid. | 2026-07-17 |
| `TownHallWindow.uss` | Stylede Town Hall som øverste kort og fordelte de øvrige bygninger i komplette rækker med tre. | 2026-07-17 |
| `TownHallWindow.uss` | Komprimerede byggegridet til fire kolonner, så Harbor-kortet er synligt og kan konstrueres eller opgraderes. | 2026-07-17 |
| `IBattleReportRepository.cs` | Tilføjede staging-operation, så worker-genererede completion reports gemmes sammen med jobmutationens ene afsluttende save. | 2026-07-17 |
| `ICityRepository.cs` | Tilføjede jobtilpasset city-load og afgrænsede standard NPC-automation til byer under pointgrænsen. | 2026-07-17 |
| `IJobRepository.cs` | Erstattede vægtet due-query med separate player- og NPC-queries samt staged jobdeletion. | 2026-07-17 |
| `IBuildingService.cs` | Tilføjede city-baseret NPC-upgrade-overload til målrettet reconciliation uden redundant genindlæsning. | 2026-07-17 |
| `ITransactionManager.cs` | Eksponerede afsluttende SaveChanges-operation til atomisk worker-persistence med præcis ét save per job. | 2026-07-17 |
| `BuildingService.cs` | Håndhævede én NPC-building i køen og genbrugte allerede målrettet indlæst reconciliation-city. | 2026-07-17 |
| `JobService.cs` | Stagede city-, research- og reportmutationer uden mellem-save samt flyttede succeslogning til debug. | 2026-07-17 |
| `CityWorker.cs` | Implementerede separate batchflows, fire parallelle aggregates, sekventiel aggregatrækkefølge, scopes, cooldown og batchtelemetri. | 2026-07-17 |
| `GameEngineWorker.cs` | Opdelte jobmotor, NPC-reconciliation, deployments og rankings i fem uafhængige fejlisolerede loops. | 2026-07-17 |
| `NPCBuildingWorker.cs` | Begrænsede reconciliation til præcis ét nyt building-job per tom NPC-kø. | 2026-07-17 |
| `TransactionManager.cs` | Implementerede workerens eksplicitte afsluttende SaveChanges gennem den eksisterende scoped GameContext. | 2026-07-17 |
| `BattleReportRepository.cs` | Tilføjede tracked report-staging uden SaveChanges til atomiske job-completion-transaktioner. | 2026-07-17 |
| `CityRepository.cs` | Tilføjede jobtilpassede EF-loads og query for NPC-byer under pointgrænsen uden aktive building-jobs. | 2026-07-17 |
| `JobRepository.cs` | Implementerede separate kronologiske due-queries, cooldown-eksklusion, no-tracking batches og staged deletion. | 2026-07-17 |
| `Program.cs` | Registrerede scope-skabende CityWorker som singleton til delte cooldowns og parallelle permanente jobloops. | 2026-07-17 |
| `CityWorkerTests.cs` | Testede parallelle spillere, streng spillerrækkefølge, ét save, rollback og fejlisolation mellem aggregates. | 2026-07-17 |
| `JobRepositoryTests.cs` | Verificerede at separat player-query er upåvirket af mere end hundrede ældre NPC-jobs. | 2026-07-17 |
| `NPCBuildingWorkerTests.cs` | Verificerede ét nyt NPC-job og ingen genopfyldning af eksisterende flerjobskøer. | 2026-07-17 |
| `ClientBuildingService.cs` | Returnerede null ved building-queue-netværksfejl, så cached kø ikke fejlagtigt tømmes. | 2026-07-17 |
| `CityStateManager.cs` | Tilføjede coalesced queue-only retry, som bevarer fejlstate og udsender autoritativ ændret kø straks. | 2026-07-17 |
| `TownHallWindowController.Queue.cs` | Viste COMPLETING og bad shared state resolvere færdige job-id'er uden at stoppe full-city refresh. | 2026-07-17 |
| `ResourceBuildingService.cs` | Afgrænsede fem-level resource projections med static datas faktiske maksimum i stedet for hardkodet level nitten. | 2026-07-17 |
| `JobRepository.cs` | Prioriterede forfaldne player jobs kronologisk før NPC jobs og lod NPC bruge resterende batchkapacitet. | 2026-07-17 |
| `UnitAvailabilityEvaluator.cs` | Gjorde manglende unlock-krav entydige med formatet Requires research-navn research uden at ændre availability-reglen. | 2026-07-17 |
| `ResearchDataGenerator.cs` | Omdøbte unit-unlocks og to utility researches samt synkroniserede descriptions og modifier source-tekster. | 2026-07-17 |
| `research.json` | Synkroniserede runtime research-displaynavne og modifier sources med generatoren uden id-, balance- eller strukturændringer. | 2026-07-17 |
| `CityTopBar.uss` | Flyttede Inventory- og Administration-ikoner tre pixels ned på desktop og tablet. | 2026-07-17 |
| `UnitStackIdeology.uss` | Gjorde current-city-navnet fuldbredt, centreret og tolv pixels uden at ændre state-bindingen. | 2026-07-17 |
| `AdministrationWindow.uss` | Indførte vægtede nul-minimumskolonner og skjulte scrollbar-chrome, så desktop-tabeller undgår horisontal overflow. | 2026-07-17 |
| `ResponsiveLayout.uss` | Bevarede phone-ikonplacering og Administrations brede gesture-scrollbare tabel trods desktop-layoutets fleksible kolonner. | 2026-07-17 |
| `ResearchWindow.uss` | Gav research-node-titler defensiv wrapping og clipping mod visuel overflow. | 2026-07-17 |
| `UnitUnlockTests.cs` | Verificerede nye displaynavne, descriptions, modifier sources, requirements, generator-synkronisering og inklusiv 22-tegnsgrænse. | 2026-07-17 |
| `ResourceBuildingServiceTests.cs` | Testede Timber Camp projections fra level atten og maksimumlevel tyve mod static data. | 2026-07-17 |
| `JobRepositoryTests.cs` | Regressionstestede player-job først i batch trods 125 ældre forfaldne NPC-building-jobs. | 2026-07-17 |
| `RecruitmentServiceTests.cs` | Opdaterede locked recruitment-forventningen til den nye entydige Bowmen research-requirement tekst. | 2026-07-17 |
| `UnitAvailabilityEvaluator.cs` | Fjernede aktuelle building-level fra låste units requirement-tekst og bevarede det autoritative krævede niveau. | 2026-07-16 |
| `UnitUnlockTests.cs` | Tilføjede regressionstest for kort building-requirement uden current-værdi i unit availability-resultatet. | 2026-07-16 |
| `ResearchWindowController.cs` | Fratrækker research points straks i delt HUD-state og refunderer lokalt ved afvist research-command. | 2026-07-16 |
| `ResearchDataGenerator.cs` | Fjernede building-reference fra genererede unit-unlock beskrivelser og bevarede level-baseret balance samt researchstruktur. | 2026-07-16 |
| `research.json` | Kortede alle femten unit-unlock beskrivelser til læsbare recruitment-tekster uden gentagne building-krav. | 2026-07-16 |
| `UnitUnlockTests.cs` | Verificerede eksakte unit-research beskrivelser uden building-navne eller requires-tekst samt fortsat generator-synkronisering. | 2026-07-16 |
| `BarracksWindow.uxml` | Skjulte body-, tab- og queue-scroller chrome samt markerede Barracks med fælles recruitment-shell. | 2026-07-16 |
| `StableWindow.uxml` | Skjulte body-, tab- og queue-scroller chrome samt markerede Stable med fælles recruitment-shell. | 2026-07-16 |
| `WorkshopWindow.uxml` | Skjulte body-, tab- og queue-scroller chrome samt markerede Workshop med fælles recruitment-shell. | 2026-07-16 |
| `HarborWindow.uxml` | Tilføjede skjult gesture-scroll, fælles recruitment-shell og responsive legacy-markører uden controller- eller gameplayændringer. | 2026-07-16 |
| `BarracksWindow.uss` | Fordelte desktop-tabs og fem queue-cards fleksibelt samt skjulte scroller rails og pile på alle recruitment-vinduer. | 2026-07-16 |
| `ResponsiveLayout.uss` | Bevarede touch-scroll med skjult chrome og tilføjede phone-layout til Harbors eksisterende legacy-indhold. | 2026-07-16 |
| `ResearchTypeEnum.cs` | Tilføjede Unlocks sidst i backendens research-kategorier, så eksisterende numeriske enumværdier forbliver stabile. | 2026-07-16 |
| `ResearchData.cs` | Udvidede research-definitioner med typed UnitRecruitment- og Subjugation-effekter samt valgfri konkret unit-type. | 2026-07-16 |
| `UnitData.cs` | Tilføjede eksplicit default-unlocked metadata til statiske unit-definitioner uden at ændre eksisterende prerequisites. | 2026-07-16 |
| `ResearchDataGenerator.cs` | Genererede fire balancerede unit-unlock branches og selvstændig Right of Subjugation med stabile id'er. | 2026-07-16 |
| `UnitDataGenerator.cs` | Markerede Militia, LightCavalry, Ballista og Longship som de eneste units åbne uden unlock-research. | 2026-07-16 |
| `research.json` | Tilføjede femten typed unit-unlocks i fire branches samt én selvstændig typed Subjugation-research. | 2026-07-16 |
| `units.json` | Markerede præcist de fire basale units som default-unlocked i den autoritative runtime-datafil. | 2026-07-16 |
| `IUnitUnlockCatalog.cs` | Definerede fælles katalogkontrakt for unit-research mappings og fremtidig server-side Subjugation-capability. | 2026-07-16 |
| `UnitUnlockCatalog.cs` | Validerede startup-kataloget mod manglende, duplikerede og uventede unit mappings samt Subjugation-effekten. | 2026-07-16 |
| `UnitAvailabilityEvaluator.cs` | Samlede building-level og completed WorldPlayer-research i ét autoritativt availability-resultat med præcise manglende krav. | 2026-07-16 |
| `ResearchDTOs.cs` | Eksponerede typed research-effekter additivt på backendens research-node DTO-kontrakt. | 2026-07-16 |
| `RecruitmentDTO.cs` | Udvidede alle fire military unit-info DTO'er additivt med serverberegnede manglende requirements. | 2026-07-16 |
| `ResearchService.cs` | Mappede statiske typed research-effekter til research tree-responsens additive effektliste. | 2026-07-16 |
| `RecruitmentService.cs` | Afviste locked recruitment med præcise krav før population, ressourcer, mutation og joboprettelse. | 2026-07-16 |
| `BarracksService.cs` | Brugte fælles availability-evaluator og inkluderede både Infantry samt Ranged i Barracks-overviewet. | 2026-07-16 |
| `StableService.cs` | Returnerede autoritativ cavalry unlock-state og manglende building- eller research-krav fra fælles evaluator. | 2026-07-16 |
| `WorkshopService.cs` | Returnerede autoritativ siege unlock-state og manglende building- eller research-krav fra fælles evaluator. | 2026-07-16 |
| `HarborService.cs` | Returnerede autoritativ naval unlock-state og manglende building- eller research-krav fra fælles evaluator. | 2026-07-16 |
| `Program.cs` | Validerede unlock-kataloget ved startup og registrerede katalog samt fælles availability-evaluator i dependency injection. | 2026-07-16 |
| `RecruitmentServiceTests.cs` | Testede locked command uden mutation og efterfølgende succes, når building samt research begge er opfyldt. | 2026-07-16 |
| `UnitUnlockTests.cs` | Testede default-unlocks, mappings, branches, availability, fire overviews og Subjugation-capability før samt efter research. | 2026-07-16 |
| `ResearchTypeEnum.cs` | Spejlede Unlocks sidst i Unitys research-enum uden at omnummerere eksisterende kategorier. | 2026-07-16 |
| `ResearchDTOs.cs` | Spejlede additive typed research-effekter inklusive nullable unit-type i Unitys transportkontrakt. | 2026-07-16 |
| `BarracksDTOs.cs` | Spejlede serverens additive UnmetRequirements-liste for Barracks units i Unity. | 2026-07-16 |
| `StableDTOs.cs` | Spejlede serverens additive UnmetRequirements-liste for Stable units i Unity. | 2026-07-16 |
| `WorkshopDTOs.cs` | Spejlede serverens additive UnmetRequirements-liste for Workshop units i Unity. | 2026-07-16 |
| `HarborDTOs.cs` | Spejlede serverens additive UnmetRequirements-liste for Harbor units i Unity. | 2026-07-16 |
| `ResearchWindowController.cs` | Bandt UNLOCKS-tabben med komplet callback-afmelding, command-disable og deferred refresh lifecycle. | 2026-07-16 |
| `ResearchWindow.uxml` | Tilføjede synlig UNLOCKS-navigation som Research-vinduets fjerde kategori. | 2026-07-16 |
| `BarracksWindowController.Recruitment.cs` | Bevarede låste tabs som inspicerbare, viste manglende krav og blokerede Barracks recruitment-controls. | 2026-07-16 |
| `StableWindowController.Recruitment.cs` | Bevarede låste tabs som inspicerbare, viste manglende krav og blokerede Stable recruitment-controls. | 2026-07-16 |
| `WorkshopWindowController.Recruitment.cs` | Bevarede låste tabs som inspicerbare, viste manglende krav og blokerede Workshop recruitment-controls. | 2026-07-16 |
| `HarborWindowController.cs` | Bevarede låste naval tabs som inspicerbare, viste manglende krav og blokerede recruitment-controls. | 2026-07-16 |
| `BarracksWindow.uss` | Stylede låste unit-tabs og requirement-tekst fælles for de fire recruitment-vinduer. | 2026-07-16 |
| `AdministrationWindowController.cs` | Fjernede manuel refresh-binding og markerede skiftevis tabelrækker uden at ændre automatisk resolving-refresh eller retry. | 2026-07-16 |
| `AdministrationWindow.uxml` | Fjernede refresh-knappen, så de to Administration-faner nu udfylder hele navigationsbredden. | 2026-07-16 |
| `AdministrationWindow.uss` | Gav tabelrækker skiftende afdæmpede overflader, kraftigere separator og tydeligere hover for klar afgrænsning. | 2026-07-16 |
| `ResponsiveLayout.uss` | Fjernede den forældede phone-regel for Administrations nu fjernede refresh-knap. | 2026-07-16 |
| `UnitDeploymentDTO.cs` | Udvidede owned deployment-responsen additivt med origin- og target-location inklusive nullable spiller- og alliancemetadata. | 2026-07-16 |
| `UnitDeploymentRepository.cs` | Loadede origin- og target-ejere med profiler og alliancer i det eksisterende no-tracking deployment-read. | 2026-07-16 |
| `UnitDeploymentService.cs` | Mappede city, koordinater, NPC, spiller og alliance til deployment-responsens nye location-felter. | 2026-07-16 |
| `UnitDeploymentRepositoryTests.cs` | Verificerede deterministisk owned-read samt indlæsning af city owner, profil, alliancenavn og tag. | 2026-07-16 |
| `UnitDeploymentServiceTests.cs` | Testede location-mapping for player cities med alliancer og ownerless NPC-target uden navigation. | 2026-07-16 |
| `UnitDeploymentDTOs.cs` | Spejlede den additive deployment-location-kontrakt med nullable id'er og deserialiserbare referencefelter i Unity. | 2026-07-16 |
| `AdministrationWindowController.cs` | Delte cached deploymentdata i movements og stationed supports med entity-links, egne empty states og korrekt timing. | 2026-07-16 |
| `AdministrationWindow.uxml` | Erstattede summary med to faner og separate centrerede movement- samt deploymenttabeller med horisontal scroll. | 2026-07-16 |
| `AdministrationDeploymentRow.uxml` | Ombyggede deploymentrækken til centrerede action-, phase-, location-, troop- og timingceller med linkcontainere. | 2026-07-16 |
| `AdministrationWindow.uss` | Komprimerede Administration-tabellerne og stylede trelinjede entity-celler, faner, semantiske actions samt stationeret layout. | 2026-07-16 |
| `WorldHexagonWindowController.cs` | Bandt det nye kompakte command-detailpanel til eksisterende missionstype og read-only target uden gameplayændringer. | 2026-07-16 |
| `WorldHexagonWindow.uxml` | Ombyggede Attack og Support til Stable-inspirerede detail-, manifest- og footerpaneler med diskret minirute. | 2026-07-16 |
| `WorldHexagonWindowCityInspection.uss` | Stylede lokalt scopede command-paneler med afdæmpede inset-flader, separators og begrænset semantisk farve. | 2026-07-16 |
| `ResponsiveLayout.uss` | Stablede command-detail og footer på phone samt bevarede Administration-scroll, rækkehøjde og touch-targets. | 2026-07-16 |
| `AdministrationWindowController.cs` | Implementerede lifecycle-sikret deploymentoversigt med summary, sortering, live countdown, resolving-refresh samt loading, empty og retry. | 2026-07-16 |
| `AdministrationWindow.uxml` | Byggede stort Administration-vindue med aktiv Troop Deployments-fane, fire summary-metrics og scrollbar deploymenttabel. | 2026-07-16 |
| `AdministrationDeploymentRow.uxml` | Definerede genbrugelig vandret deploymentrække med mission, phase, route, troop manifest og timingkolonner. | 2026-07-16 |
| `AdministrationWindow.uss` | Stylede Administration-vinduets summary, semantiske missioner, tabelkolonner og resolving-state med det globale theme. | 2026-07-16 |
| `Window_Administration.prefab` | Forbandt Administration UIDocument, controller og deployment-row-template i et nyt gameplayvindue-prefab. | 2026-07-16 |
| `WindowTypeEnum.cs` | Tilføjede Administration som stabil frontend-lokal vinduestype med numerisk værdi 29. | 2026-07-16 |
| `00_Bootstrap.unity` | Registrerede Administration-prefab for vinduestype 29 i den persistente globale window manager. | 2026-07-16 |
| `WindowTooltips.txt` | Tilføjede katalogtekst om Administrations read-only oversigt over aktive troop deployments og live timing. | 2026-07-16 |
| `ClientUnitDeploymentService.cs` | Udvidede aktive deployment-readet bagudkompatibelt med valgfri error callback til skelnen mellem tomt resultat og fejl. | 2026-07-16 |
| `UnitDeploymentRepository.cs` | Gjorde aktive deployment-reads no-tracking og deterministisk sorterede på bevægelses-ETA samt stationeringstid. | 2026-07-16 |
| `UnitDeploymentService.cs` | Ensrettede deploymentoversigtens deterministiske sortering med bevægelser før nyeste stationerede supports. | 2026-07-16 |
| `UnitDeploymentRepositoryTests.cs` | Testede owned outbound, returning og stationed support, fremmed udelukkelse, mapping samt deterministisk displayrækkefølge. | 2026-07-16 |
| `UnitDeploymentServiceTests.cs` | Rettede deployment-repositorydoublen og verificerede at moving samt stationed deployments returneres i displayrækkefølge. | 2026-07-16 |
| `WorldHexagonWindowController.cs` | Bandt mission-badge og transportmetric til eksisterende estimate-, submit-, permission- og refresh-flow. | 2026-07-16 |
| `WorldHexagonWindow.uxml` | Ombyggede Attack og Support til command-layout med mission, rute, metrics, manifest, status og troop-total. | 2026-07-16 |
| `WorldHexagonWindowCityInspection.uss` | Stylede City Inspections pseudo-grafiske command-layout og semantiske Attack- samt Support-states lokalt. | 2026-07-16 |
| `ResponsiveLayout.uss` | Stablede command-rute, metrics og Administration-summary på phone samt reserverede plads til ny HUD-knap. | 2026-07-16 |
| `CityTopBar.uxml` | Placerede funktionel Admin-knap med eksisterende eye-ikon umiddelbart til højre for Inventory. | 2026-07-16 |
| `CityTopBar.uss` | Genbrugte Inventory-knappens HUD-udtryk og stylede Administrations eye-ikon uden ny palette. | 2026-07-16 |
| `CityTopBarViewController.cs` | Bandt Administration-knappen lifecycle-sikkert til GlobalWindowManager og det registrerede vindue. | 2026-07-16 |
| `BarracksWindow.uss` | Komprimerede recruitment-vinduernes lodrette panelafstande, så desktop- og tabletindholdet passer uden en utilsigtet vertikal scrollbar. | 2026-07-16 |
| `WorkshopWindowController.Recruitment.cs` | Beregnede Workshops population-total, samlede jobtid og løbende UTC-ETA, fjernede slotnummer og ændrede kortlabelen til AMOUNT. | 2026-07-16 |
| `WorkshopWindowController.cs` | Bandt Workshops nye population-, total recruitment time- og completion ETA-felter til controllerens cachede UI-referencer. | 2026-07-16 |
| `StableWindowController.Recruitment.cs` | Beregnede Stables population-total, samlede jobtid og løbende UTC-ETA, fjernede slotnummer og ændrede kortlabelen til AMOUNT. | 2026-07-16 |
| `StableWindowController.cs` | Bandt Stables nye population-, total recruitment time- og completion ETA-felter til controllerens cachede UI-referencer. | 2026-07-16 |
| `BarracksWindowController.Recruitment.cs` | Beregnede Barracks population-total, samlede jobtid og løbende UTC-ETA, fjernede slotnummer og ændrede kortlabelen til AMOUNT. | 2026-07-16 |
| `BarracksWindowController.cs` | Bandt Barracks nye population-, total recruitment time- og completion ETA-felter til controllerens cachede UI-referencer. | 2026-07-16 |
| `ResponsiveLayout.uss` | Tilpassede fire cost-kolonner, timingfelter og lavere queue-cards til recruitment-vinduernes kompakte phone-layout. | 2026-07-16 |
| `BarracksWindow.uss` | Centrerede stats vertikalt, stylede population og timingoversigt samt reducerede aktive recruitment-cards højde. | 2026-07-16 |
| `WorkshopWindow.uxml` | Tilføjede population-cost, samlet recruitment time og completion ETA til Workshops fælles action-panel. | 2026-07-16 |
| `StableWindow.uxml` | Tilføjede population-cost, samlet recruitment time og completion ETA til Stables fælles action-panel. | 2026-07-16 |
| `BarracksWindow.uxml` | Tilføjede population-cost, samlet recruitment time og completion ETA til Barracks fælles action-panel. | 2026-07-16 |
| `WorkshopWindowController.Recruitment.cs` | Fjernede generering af tomme Workshop-slots, så kun serverordnede aktive recruitment-jobs vises i køen. | 2026-07-16 |
| `StableWindowController.Recruitment.cs` | Fjernede generering af tomme Stable-slots, så kun serverordnede aktive recruitment-jobs vises i køen. | 2026-07-16 |
| `BarracksWindowController.Recruitment.cs` | Fjernede generering af tomme Barracks-slots, så kun serverordnede aktive recruitment-jobs vises i køen. | 2026-07-16 |
| `ResponsiveLayout.uss` | Fastholdt kompakt phone-højde for recruitment-køen, mens desktop og tablet kan udnytte fleksibel panelhøjde. | 2026-07-16 |
| `BarracksWindow.uss` | Ensrettede recruitment-paneler, stablede unit-headeren lodret og lod aktive queue-cards udfylde resterende vindueshøjde. | 2026-07-16 |
| `WorkshopWindow.uxml` | Markerede detail-, action- og queue-sektionerne med den lokalt scopede fælles recruitment-panelklasse. | 2026-07-16 |
| `StableWindow.uxml` | Markerede detail-, action- og queue-sektionerne med den lokalt scopede fælles recruitment-panelklasse. | 2026-07-16 |
| `BarracksWindow.uxml` | Markerede detail-, action- og queue-sektionerne med den lokalt scopede fælles recruitment-panelklasse. | 2026-07-16 |
| `WorkshopWindowController.Recruitment.cs` | Standardiserede Workshop-rekruttering, separate costs og capacity samt fem slots med lifecycle-sikret countdown og progress. | 2026-07-16 |
| `WorkshopWindowController.cs` | Bandt Workshops separate resource-costfelter og Unit Capacity-metric til det nye recruitment-layout. | 2026-07-16 |
| `StableWindowController.Recruitment.cs` | Renderede Stable-tabs, separate costs og fem faste queue-slots med countdown, READY-state og progress. | 2026-07-16 |
| `StableWindowController.cs` | Bandt Stables separate resource-costfelter og Unit Capacity-metric til det nye recruitment-layout. | 2026-07-16 |
| `BarracksWindowController.Recruitment.cs` | Renderede Barracks-tabs, separate costs og fem faste queue-slots med countdown, READY-state og progress. | 2026-07-16 |
| `BarracksWindowController.cs` | Bandt Barracks separate resource-costfelter og Unit Capacity-metric til det nye recruitment-layout. | 2026-07-16 |
| `ResponsiveLayout.uss` | Stablede recruitment-command-indhold på telefon og muliggjorde vertikal body-scroll samt horisontal queue-swipe. | 2026-07-16 |
| `BarracksWindow.uss` | Tilføjede scoped professionelt command-design til Barracks, Stable og Workshop uden at ændre Harbors legacy-layout. | 2026-07-16 |
| `WorkshopWindow.uxml` | Erstattede construction-terminologi med recruitment-command-layout, ni metrics, ikonbaserede costs og fem-slot queue. | 2026-07-16 |
| `StableWindow.uxml` | Ombyggede Stable til fælles recruitment-command-layout med ni metrics, ikonbaserede costs og scrollbar queue. | 2026-07-16 |
| `BarracksWindow.uxml` | Ombyggede Barracks til fælles recruitment-command-layout med ni metrics, ikonbaserede costs og scrollbar queue. | 2026-07-16 |
| `Unity/Assets/_Project/Scripts/Modules/UI/CityTopBarViewController.ExoticResources.cs` | Synkroniserede exotic-tooltippens viewport-fallback med den bredere layoutbredde, så cursorpositionering fortsat clampes korrekt. | 2026-07-16 |
| `Unity/Assets/UI/HUD/CityTopBar.uss` | Udvidede exotic-tooltippen, så fulde resourcenavne vises uden at forskyde de låste værdi- og ratekolonner. | 2026-07-16 |
| `Unity/Assets/UI/HUD/CityTopBar.uss` | Låste exotic-værdikolonner og centrerede Inventory-indholdet bedre vertikalt i knappen. | 2026-07-16 |
| `TownHallWindow.uss` | Fjernede lokal tooltip-border, så Town Hall-tooltippen arver den fælles firefarvede vinduesramme. | 2026-07-15 |
| `TownHallWindow.uxml` | Markerede construction resource-tooltippen som en fælles flydende vinduesramme med eksisterende indhold og positionering. | 2026-07-15 |
| `ResearchWindow.uss` | Fjernede Research lock-tooltippens lokale ensfarvede border-override og bevarede dens featurespecifikke layout. | 2026-07-15 |
| `ResearchWindow.uxml` | Markerede Research lock-tooltippen som fælles flydende vinduesramme uden at ændre interaktionen. | 2026-07-15 |
| `BaseWindow.cs` | Gav runtime-genererede window-info-tooltips fælles ydre vinduesramme og flydende surface-markør. | 2026-07-15 |
| `WindowTheme.uss` | Gjorde window-frame til eneste autoritative ydre border for vinduer og flydende info-tooltips. | 2026-07-15 |
| `ResponsiveLayout.uss` | Bevarede firefarvet phone-border, omplacerede Inventory mellem map og bynavn samt stablede exotic-rækker. | 2026-07-15 |
| `CityTopBarViewController.ExoticResources.cs` | Opdelte exotic beholdning og rate i separate felter samt præciserede city-produktionslabelen for lagrede ressourcer. | 2026-07-15 |
| `CityTopBar.uss` | Gjorde Inventory flex-baseret, ensrettede flydende rammer og justerede lige exotic beholdnings- og ratekolonner. | 2026-07-15 |
| `CityTopBar.uxml` | Flyttede Inventory efter server time og markerede dropdown samt HUD-tooltips som fælles flydende rammer. | 2026-07-15 |
| `UnitStackIdeologyController.cs` | Synkroniserede current-city-navnet straks og via CityStateManager-event med korrekt subscription-lifecycle. | 2026-07-15 |
| `UnitStackIdeology.uss` | Gav Focuses samme 3 px hover- og 1 px active-bevægelse som sidebar-knapperne. | 2026-07-15 |
| `UnitStackIdeology.uxml` | Erstattede statisk current-city-tekst med et navngivet tomt felt til aktiv bystate. | 2026-07-15 |
| `ResourceServiceProductionTests.cs` | Testede separat unit- og building-upkeep samt præcis summering af alle globale city-produktionsbidrag. | 2026-07-15 |
| `CityServiceTownHallTests.cs` | Testede detailed-city mapping af produktion og population inklusive recruitment queue og clamped rest. | 2026-07-15 |
| `BuildingServiceTests.cs` | Udvidede resource service-testdoublen med city-production-kontrakten uden at ændre eksisterende testadfærd. | 2026-07-15 |
| `JobServiceReportTests.cs` | Udvidede resource service-testdoublen med city-production-kontrakten uden at ændre eksisterende testadfærd. | 2026-07-15 |
| `RecruitmentServiceTests.cs` | Udvidede resource service-testdoublen med city-production-kontrakten uden at ændre eksisterende testadfærd. | 2026-07-15 |
| `WorldPlayerServiceTests.cs` | Udvidede resource service-testdoublene med city-production-kontrakten uden at ændre eksisterende testadfærd. | 2026-07-15 |
| `IResourceService.cs` | Tilføjede fælles city-production-snapshotkontrakt mellem autoritativ global økonomi og HUD-visning. | 2026-07-15 |
| `ResourceService.cs` | Genbrugte city-snapshots til netto-Gold, modifieret Research og præcist fordelte Ideology-bidrag. | 2026-07-15 |
| `CityDTO.cs` | Udvidede detailed-city-responsen additivt med globale produktionsbidrag og et fuldt population-breakdown. | 2026-07-15 |
| `CityService.cs` | Mappede autoritative city-produktionssnapshots og population inklusive resterende recruitment queue til detailed-city. | 2026-07-15 |
| `CityDTOs.cs` | Spejlede detailed-city-felter for globale produktionsbidrag og fuldt population-breakdown i Unity. | 2026-07-15 |
| `CityResourceState.cs` | Tilføjede autoritativ city-produktion og population-breakdown til Unitys delte city-resource-state. | 2026-07-15 |
| `CityStateManager.cs` | Mappede nye autoritative produktions- og populationfelter fra polling-responsen til shared state. | 2026-07-15 |
| `CityTopBarViewController.cs` | Viste serverens clamped remaining population og bandt nye tooltipfelter til topbar-controlleren. | 2026-07-15 |
| `CityTopBarViewController.ExoticResources.cs` | Renderede city/global rates, dedikeret population-breakdown og grupperede exotic-beholdninger med ø-produktion. | 2026-07-15 |
| `CityTopBar.uxml` | Tilføjede semantiske produktionslabels og et dedikeret population-tooltip med fem autoritative rækker. | 2026-07-15 |
| `CityTopBar.uss` | Indrammede Inventory, udvidede responsive exotic-kolonner og stylede det dedikerede population-tooltip. | 2026-07-15 |
| `LeftSideBar.uxml` | Erstattede sidebarens ScrollView med en almindelig container uden scrolling eller scrollbarstruktur. | 2026-07-15 |
| `LeftSideBar.uss` | Fjernede alle ScrollView-specifikke selectors fra den nye almindelige sidebar-container. | 2026-07-15 |
| `ResponsiveLayout.uss` | Fordelte alle ti telefonknapper ligeligt på én 44-pixel række uden scrolling. | 2026-07-15 |
| `CityTopBar.uxml` | Flyttede Inventory ud af ressourcerækken og placerede den separat umiddelbart til venstre for logout. | 2026-07-15 |
| `CityTopBar.uss` | Fjernede Inventory-knappens kasse, gav ikonet luft og reserverede topbarplads ved logout. | 2026-07-15 |
| `UnitStackIdeology.uss` | Flyttede current-city-divideren under Report Bug og ensrettede Focuses samt unit-rækkernes diskrete kantstyling. | 2026-07-15 |
| `ResponsiveLayout.uss` | Placerede Inventory ved logout på telefon og reserverede tilsvarende plads i city selector. | 2026-07-15 |
| `CityTopBar.uxml` | Tilføjede en visuel Inventory-knap med chest-ikon i topbarens eksisterende ressourceområde uden klikbinding. | 2026-07-15 |
| `CityTopBar.uss` | Stylede Inventory-knappen og chest-ikonet konsekvent med topbarens mørke grand-strategy HUD-udtryk. | 2026-07-15 |
| `LeftSideBar.uxml` | Indsatte Starter Quest som ny visuel sidebar-knap uden controllerbinding eller vinduesnavigation. | 2026-07-15 |
| `LeftSideBar.uss` | Flyttede chest-ikonet til Inventory, gav Dailies present-ikonet og tilføjede Quest-ikon til Starter Quest. | 2026-07-15 |
| `ResponsiveLayout.uss` | Tilpassede Inventory-knappen til topbarens kompakte telefonrække og skjulte dens tekstlabel på små skærme. | 2026-07-15 |
| `WindowTypeEnum.cs` | Tilføjede Dailies som stabil vinduestype med værdi 28 uden at ændre eksisterende enumværdier. | 2026-07-15 |
| `LeftSideBar.uxml` | Indsatte Dailies først og gjorde telefonnavigationen kompatibel med en vandret scrollbar. | 2026-07-15 |
| `LeftSideBar.uss` | Komprimerede desktopknapper, tilføjede chest-ikon og gav Dailies den eksisterende gyldne HUD-farve. | 2026-07-15 |
| `LeftSideBarViewController.cs` | Bandt Dailies-navigationen til GlobalWindowManager med samme registrerede callback-lifecycle som eksisterende knapper. | 2026-07-15 |
| `ResponsiveLayout.uss` | Gjorde telefonens ni sidebar-knapper vandret scrollbare med faste touch targets og responsiv claim-højde. | 2026-07-15 |
| `DailiesWindowController.cs` | Oprettede 20 placeholder-rækker og en unscaled UTC-countdown, som stoppes ved lukning. | 2026-07-15 |
| `DailiesWindow.uxml` | Byggede Dailies-vinduets header, reset-panel og scrollbar tabel med fem specificerede kolonner. | 2026-07-15 |
| `DailiesWindow.uss` | Stylede 1100 gange 720 vinduet, reset-panelet, procentkolonner og den responsive minimumsbredde. | 2026-07-15 |
| `DailiesRow.uxml` | Definerede genbrugelig tabelrække med level, tre placeholderfelter og deaktiveret grøn claim-knap. | 2026-07-15 |
| `Window_Dailies.prefab` | Forbandt Dailies UIDocument, controller og rækketemplate i et nyt gameplayvindue-prefab. | 2026-07-15 |
| `00_Bootstrap.unity` | Registrerede Window_Dailies-prefab for vinduestype 28 i den persistente globale window manager. | 2026-07-15 |
| `WindowTooltips.txt` | Tilføjede katalogtekst, der forklarer Dailies-shellens UTC-reset og endnu inaktive gameplayfunktioner. | 2026-07-15 |
| `daily-objectives.json` | Tilføjede tyve fortløbende server-side definitionspladser med tomme objective-, completion- og rewardfelter. | 2026-07-15 |
| `Game.csproj` | Konfigurerede daily-objectives JSON til at blive kopieret uændret til backendens build-output. | 2026-07-15 |
