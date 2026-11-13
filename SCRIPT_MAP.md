# Карта скриптов по механикам

> Обновлено после реорганизации: часть файлов переименована по классам, основные скрипты из `Scripts/` разложены в `Scripts/Core`, `Scripts/Player`, `Scripts/Level`, `Scripts/Objects`, `Scripts/Menus`. Пути ниже относятся к состоянию до этого. Актуальные пути смотрите в проводнике.

Черновик для будущей реорганизации. Файлы пока не перемещены и не переименованы.
Для переименования нужно менять имя файла, имя класса и `.meta` вместе, иначе Unity потеряет ссылки.

## 1. Игрок и управление
| Файл | Класс | Что делает |
|---|---|---|
| Scripts/PlayerController.cs | PlayerController | Основной контроллер игрока (режимы Cube/Ship/Wave/UFO/Spider/Ball) |
| Scripts/ModeChanger.cs | ModeChanger | Смена режима игрока |
| Scripts/CameraController.cs | CameraController | Камера, следует за игроком |
| Scripts/CameraEdit.cs | CameraEdit | Камера в редакторе уровней (зум, касания) |
| Scripts/Reset.cs | SceneLoaderV2 | Перезапуск уровня при смерти |

## 2. Прыжки, орбы и падения (OrbPadsJump)
| Файл | Класс |
|---|---|
| OrbPadsJump/Orb.cs | Orb |
| OrbPadsJump/GravityOrb.cs | GravityOrb |
| OrbPadsJump/GravityPad.cs | GravityPad |
| OrbPadsJump/JumpPad.cs | JumpPad |
| OrbPadsJump/SpeedChanger.cs | SpeedChanger |
| Prefabs/RotatorJumo.cs | ObjectRotator2D (вращение объектов) |

## 3. Монеты, звёзды, очки
| Файл | Класс |
|---|---|
| PointCollector.cs | PointCollector |
| gETS/EffectCollecting.cs | ObjectSplitter2D (эффект разлёта при сборе) |
| Scripts/StarManager.cs | StarManager |
| Score/ScoreManager.cs | ScoreManager |
| Scenes/LeaderBoard/ScoreSystem.cs | ScoreSystem |
| Accounts/StarAdding.cs | StarAdding |

## 4. Старт, финиш, переходы уровней
| Файл | Класс |
|---|---|
| Scripts/FinishTrigger.cs | FinishTrigger |
| Scripts/Finish.cs | Finish |
| Gaunlet/FinishTrigger.cs | Finish2D (имя файла не совпадает с классом, дубль по смыслу) |
| Scenes/LeaderBoard/FinishPoint.cs | FinishPoint1 |
| BackGround/FinishHandler.cs | FinishHandler2D |
| iconMENU/LevelCompletionTrigger.cs | LevelCompletionTrigger |
| Scripts/LevelTransition.cs | LevelTransition |
| Scripts/ExitToMainMenu.cs | ExitToMainMenu |
| Score/SceneReloader.cs | SceneReloader |
| Prefabs/ЫВывы.cs | PracticeClearZone (имя файла на кириллице) |
| Scripts/LevelGenerator.cs | LevelGenerator |

## 5. Объекты уровня и переключатели
| Файл | Класс |
|---|---|
| Scripts/Object Toggler.cs | ObjectToggler (пробел в имени файла) |
| Scripts/ObjectToggleOnCollision.cs | ObjectToggleOnCollision |
| Scripts/TG.cs | MultiObjectManager |
| Prefabs/TOGGLETRU.cs | GroupShowHideController |
| aSKINS/ToggleObjectsVisibility.cs | ToggleObjectsGroups |
| MarketPlace/UIObjectDisable.cs | SwitchObjectsUniversal |
| ProtectedObject.cs | ProtectedObject |
| PL.cs | ObjectInteraction2D (имя файла не совпадает с классом) |
| Prefabs/Scream.cs | Scream |
| Prefabs/SC.cs | SelectablePulse (имя файла непонятное) |

## 6. Визуал, фон, цвета
| Файл | Класс |
|---|---|
| BackGround/ColorChanger.cs | ColorController |
| BackGround/TextureChanger.cs | TextureChanger2D |
| NewBAg/BGPicker.cs | BackgroundPicker |
| ColorKeeper.cs | ColorKeeper |
| Fff.cs | Fff (непонятное назначение, проверить) |

## 7. Редактор уровней (dev-инструменты)
| Файл | Класс | Замечание |
|---|---|---|
| Scripts/LevelCreator.cs | LevelCreator | 3343 строки, самый большой файл |
| Scripts/LevelBuilder.cs | LevelBuilder | |
| Editor/LevelEditor.cs | LevelBuilder | **Дубль имени класса с Scripts/LevelBuilder.cs** |
| Editor/EditorObject.cs | EditorObject | |
| Editor/controleditcamer.cs | CameraController | **Дубль имени класса с Scripts/CameraController.cs** |
| Editor/LevelMenu.cs | LevelMenu | **Дубль имени класса с Scripts/LevelMenu.cs** |

## 8. Меню, навигация, сцены
| Файл | Класс |
|---|---|
| Scripts/GameManager.cs | GameManager (ядро: прогресс, PlayerPrefs) |
| Scripts/UIManager.cs | UIManager |
| Scripts/LevelMenu.cs | LevelMenu (список локальных уровней) |
| Gaunlet/LevelManager.cs | GameData (имя файла не совпадает с классом) |
| Gaunlet/UILevelButton.cs | FixedLevelButton |
| Gaunlet/MenuUpdater.cs | MenuController |
| Loader.cs | SceneLoader |
| Scripts/SceneLoaderByID.cs | SceneLoaderByID |
| Scripts/SwitcherScene.cs | SceneSwitcher |
| Scripts/UltimateSceneLoader.cs | UltimateSceneLoader |
| Scenes/KeyboardSceneSwitcher.cs | KeyboardSceneSwitcher |
| Scripts/UnbreakableButton.cs | UnbreakableButton |
| ButtonClicker.cs | ButtonClicker |
| Scenes/url.cs | OpenURL |
| Scenes/LeaderBoard/uimonet.cs | UIMovement |
| Scenes/MusicSelectore.cs | MusicSelector (опечатка в имени файла) |

## 9. Уровни (онлайн и локальные)
| Файл | Класс |
|---|---|
| MarketPlace/MarketplaceManager.cs | SupabaseManager |
| MarketPlace/MarketplaceUI.cs | MarketplaceUI |
| MarketPlace/LevelEntryUI.cs | MarketplaceLevelEntryUI |
| MarketPlace/LocalEntryUI.cs | LocalLevelEntry |
| MarketPlace/LocalLevelData.cs | LocalLevelMeta |
| Scripts/LevelCreator.cs (внутри) | LocalLevelData (вложенный класс) |

## 10. Аккаунты, медали, демоны
| Файл | Класс |
|---|---|
| aSKINS/AccountsManager.cs | AccountsManager |
| aSKINS/AccountUI.cs | AccountUI |
| aSKINS/AccountItemUI.cs | AccountItemUI |
| Accounts/TopPlayerItemUI.cs | TopPlayerItemUI |
| Accounts/mEDALAS/MedalsManager.cs | MedalsManager |
| Accounts/mEDALAS/DetailAccountPanel.cs | DetailAccountPanel |
| Accounts/DemonsManager.cs | DemonManager |
| Gaunlet/AddDemon.cs | AddDemon |

## 11. Скины и иконки
| Файл | Класс | Замечание |
|---|---|---|
| aSKINS/IconManager (2).cs | SkinManager | Имя файла с "(2)", похоже на копию |
| aSKINS/IconSelector.cs | SkinSelectorUI | |
| iconMENU/UnlockManager.cs | UnlockManager | |

## 12. Квесты и события
| Файл | Класс |
|---|---|
| Quests/QuestsManage.cs | QuestsManager |
| Quests/QuestPrefabController.cs | QuestPrefabController |
| Quests/QuestionProggresive.cs | QuestProgressTracker |
| Quests/Scroller.prefab | (префаб, не скрипт) |
| Events/Events.cs | EventsSystemManager |
| Events/EventItemUI.cs | EventItemUI |
| Events/EventDatas.cs | EventData, DepositRecord |

## 13. Промокоды, локализация
| Файл | Класс |
|---|---|
| pROM.cs | PromoCodeManager (имя файла на латинице с ошибкой) |
| aTranslator/Translator.cs | TranslationManager |
| aTranslator/TransbleText.cs | TranslatableText (опечатка в имени файла) |

## 14. Настройки
| Файл | Класс |
|---|---|
| set/game-settings/.../GameSettingsManager.cs | GameSettingsManager |
| set/game-settings/.../SoundVolumeUpdater.cs | SoundVolumeUpdater |
| set/game-settings/.../UI/*.cs | FullscreenToggle, QualityDropdown, ResolutionDropdown, ScreenModeDropdown, SoundVolumeSlider |
| set/game-settings-sample/Scripts/Sample.cs | Sample (пример из пакета, можно удалить) |

## Дубли и подозрительные файлы
- Дубли имён классов: `LevelBuilder` (Scripts и Editor), `CameraController` (Scripts и Editor), `LevelMenu` (Scripts и Editor).
- Файлы с именем, не совпадающим с классом: `Gaunlet/FinishTrigger.cs`, `Gaunlet/LevelManager.cs`, `PL.cs`. Unity не сможет добавить такой компонент на объект.
- MonoBehaviour в папке `Editor/` работают только в редакторе, их нельзя повесить на объект в игре.
- `ProjectSettings.zip`, `Assets.zip` и `Assembly-CSharp*.csproj` в корне — артефакты, их можно не трогать.
- `Scripts/Scroll View.prefab`, `Quests/Scroller.prefab` — префабы скролла, не скрипты.
