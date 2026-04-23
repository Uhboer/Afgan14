game-ticker-restart-round = IT'S OVER...
game-ticker-start-round = IS NEAR...
game-ticker-start-round-cannot-start-game-mode-fallback = Не удалось запустить режим { $failedGameMode }! Запускаем { $fallbackMode }...
game-ticker-start-round-cannot-start-game-mode-restart = Не удалось запустить режим { $failedGameMode }! Перезапуск раунда...
game-ticker-start-round-invalid-map = Выбранная карта { $map } не подходит для игрового режима { $mode }. Игровой режим может не функционировать как задумано...
game-ticker-unknown-role = Неизвестный
game-ticker-delay-start = Начало раунда было отложено на { $seconds } секунд.
game-ticker-pause-start = Начало раунда было приостановлено.
game-ticker-pause-start-resumed = Отсчёт начала раунда возобновлён.
game-ticker-player-join-game-message = ДОБРОЕ УТРО, МУСОР, ТЫ В АФГАНЕ!
game-ticker-get-info-text =
    ТЫ ОПОЗДАЛ НА ЗАПУСК [color=white]АФГАНА[/color], СОСУНОК
    Раунд: [color=white]#{ $roundId }[/color]
    Братва: [color=white]{ $playerCount }[/color]
    Период: [color=white]{ $mapName }[/color]
game-ticker-get-info-preround-text =
    ТЫ НА [color=white]АФГАНЕ[/color], СОСУНОК
    Раунд: [color=white]#{ $roundId }[/color]
    Братва: [color=white]{ $playerCount }[/color] ([color=white]{ $readyCount }[/color] { $readyCount ->
        [one] готов
       *[other] готовы
    })
    Период: [color=white]{ $mapName }[/color]
game-ticker-no-map-selected = [color=red]Период ещё не выбран![/color]
game-ticker-player-no-jobs-available-when-joining = При попытке присоединиться к игре ни одной роли не было доступно.
# Displayed in chat to admins when a player joins
player-join-message = Игрок { $name } зашёл!
player-first-join-message = Игрок { $name } зашёл на сервер впервые.
# Displayed in chat to admins when a player leaves
player-leave-message = Игрок { $name } вышел!
latejoin-arrival-announcement =
    { $character } ({ $job }) { $gender ->
        [male] пробудился
        [female] пробудилась
        [epicene] пробудились
       *[neuter] пробудился
    } в Афгане!
latejoin-arrival-sender = Афган
latejoin-arrivals-direction = Ты пробудился поздно. Сейчас ты пешере пробудившихся. Иди по указаниям и тропинкам у выхода с пещеры на свою базу.
latejoin-arrivals-direction-time = Ты пробудился поздно. Сейчас ты пешере пробудившихся. Иди по указаниям и тропинкам у выхода с пещеры на свою базу.
preset-not-enough-ready-players = Не удалось запустить пресет { $presetName }. Требуется { $minimumPlayers } игроков, но готовы только { $readyPlayersCount }.
preset-no-one-ready = Не удалось запустить режим { $presetName }. Нет готовых игроков.
