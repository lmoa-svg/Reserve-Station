genetics-console-chat-scanning = СКАНИРОВАНИЕ СУБЪЕКТА
genetics-console-chat-scan-failed = СКАНИРОВАНИЕ СУБЪЕКТА НЕ УДАЛОСЬ
genetics-console-chat-scanned = НОВЫЙ СУБЪЕКТ ОТСКАНИРОВАН
genetics-console-chat-sequencing = СЕКВЕНИРОВАНИЕ ГЕНОМА
genetics-console-chat-sequence-failed = НЕ УДАЛОСЬ СЕКВЕНИРОВАТЬ ГЕНОМ
genetics-console-chat-genetic-damage = ГЕНОМ ПОВРЕЖДЁН СЛИШКОМ СИЛЬНО ДЛЯ ЧТЕНИЯ
genetics-console-chat-sequenced = СЕКВЕНИРОВАНИЕ ГЕНОМА ЗАВЕРШЕНО
genetics-console-chat-combining = ОБЪЕДИНЕНИЕ МУТАЦИЙ
genetics-console-chat-combine-failed = ОБЪЕДИНЕНИЕ МУТАЦИЙ ПРЕРВАНО
genetics-console-chat-combine-none = ОБЪЕДИНЕНИЙ НЕ НАЙДЕНО
genetics-console-chat-combine-present = ОБЪЕДИНЕНИЕ УЖЕ ПОЛУЧЕНО
genetics-console-chat-combined = ОБЪЕДИНЕНИЕ МУТАЦИЙ ЗАВЕРШЕНО
genetics-console-chat-applying-enzymes = ПРИМЕНЕНИЕ УНИКАЛЬНЫХ ФЕРМЕНТОВ
genetics-console-chat-apply-enzymes-failed = НЕ УДАЛОСЬ ПРИМЕНИТЬ УНИКАЛЬНЫЕ ФЕРМЕНТЫ
genetics-console-chat-applied-enzymes = УНИКАЛЬНЫЕ ФЕРМЕНТЫ ПРИМЕНЕНЫ

genetics-console-radio-message = {CAPITALIZE($mutation)} секвенирована: на сервер НИО добавлено {$points} очков!

genetics-console-damages-you = Секвенирование катастрофически проваливается и повреждает ваш геном!
genetics-console-damages-others = Генетическая консоль катастрофически проваливается и повреждает геном субъекта!

genetics-console-linking-you = {CAPITALIZE($user)} начинает привязывать вас к {$scanner}
genetics-console-linking-others = {CAPITALIZE($user)} начинает привязывать {$target} к {$scanner}
genetics-console-linked = Цель успешно привязана


genetics-console-window-title = Генетическая консоль 3000
genetics-scanner-window-title = Генетический сканер 4000

genetics-console-tab-sequencer = Секвенатор
genetics-console-tab-storage = Хранилище
genetics-console-tab-combiner = Комбинатор
genetics-console-tab-enzymes = Ферменты

genetics-console-heading-scanner = Медицинский сканер
genetics-console-no-scanner = Медицинский сканер не подключён!
genetics-console-no-subject = В медицинском сканере не обнаружено субъекта.
genetics-console-name = Имя
genetics-console-status = Статус
genetics-console-integrity = Целостность
genetics-console-instability = Нестабильность
genetics-console-scramble = Перемешать ДНК
genetics-console-scramble-cooldown = Перемешивание перезаряжается ({$cooldown} с)

genetics-console-inserted-disk = Вставленный диск:
genetics-console-loaded-mutation = Загруженная мутация:
genetics-console-no-disk = Диск не вставлен
genetics-console-print-item = Напечатать {$item} ({$cost})

genetics-console-sequencer-no-subject = Субъект не обнаружен
genetics-console-sequencer-not-scanned = Отсканируйте субъекта, чтобы начать.
genetics-console-sequencer-no-sequences = ДНК субъекта пуста!
genetics-console-sequences = Последовательности
genetics-console-sequence-info = Сведения о последовательности
genetics-console-scan = Сканировать!
genetics-console-print-scan = Напечатать скан
genetics-console-genome-sequencer = Генетический секвенатор™
genetics-console-sequencer-no-sequence-selected = Выберите последовательность для работы.
genetics-console-sequence-text = [{$rarity}] {$number}
genetics-console-mutation-name = Название:
genetics-console-mutation-desc = Описание:
genetics-console-mutation-instability = Нестабильность:
genetics-console-mutation-name-placeholder = Мутация {$number}
genetics-console-write-mutation = Сохранить на диск
genetics-console-print-sequence = Напечатать основания
genetics-console-sequencer-tip = Подсказка: Ctrl+ЛКМ ставит основание X. ПКМ переключает в обратном порядке.
genetics-console-begin-sequencing = Начать секвенирование
genetics-console-reset-sequence = Сбросить последовательность

genetics-console-examined = В консоли хранится [bold]{$biomass}[/bold] биомассы.
genetics-console-missing-biomass = В консоли недостаточно биомассы, чтобы это напечатать!

genetics-console-combine-button = Объединить!
genetics-console-combine-catalyst = Мутация-катализатор: {$mutation}
genetics-console-combine-results = С помощью этой мутации можно получить: {$results}!
genetics-console-combine-no-results = Эту мутацию нельзя ни с чем объединить.
genetics-console-disk-empty = На диске нет мутации!

genetics-console-scanned-mob = Отсканированное существо
genetics-console-save-enzymes = Сохранить на диск
genetics-console-disk-enzymes = Уникальные ферменты на диске
genetics-console-apply-enzymes = Применить ферменты к субъекту
genetics-console-color = R{$r}/G{$g}/B{$b}
genetics-console-enzymes-current-disk = Текущая дискета:
genetics-console-enzymes-stored = Сохранённые ферменты:
genetics-console-enzymes-sex = Пол:
genetics-console-enzymes-eye-color = Цвет глаз:
genetics-console-enzymes-skin-color = Цвет кожи:

ent-ComputerGenetics = консоль генетики
    .desc = Компьютер, используемый для расшифровки, изоляции и модификации биологических геномов и уникальных энзимов.
ent-GeneticsComputerCircuitboard = печатная плата консоли генетики
    .desc = Печатная плата для сборки консоли генетики.
ent-HandheldGeneticsScanner = портативный сканер генома
    .desc = Портативное устройство для забора и считывания ДНК, способное отображать все последовательности в субъекте вне лаборатории.
ent-GeneticsDisk = генетический диск
    .desc = Диск хранения данных, способный кодировать одну мутацию для использования в консоли генетики. Не подвергать микроволновому излучению.
ent-GeneticsDiskUnstableDna = генетический диск (нестабильная ДНК)
    .desc = Диск хранения данных, содержащий мутацию нестабильной ДНК.
ent-GeneticsDiskMouse = генетический диск (мышь)
    .desc = Диск хранения данных, содержащий мутацию превращения в мышь.
ent-PaperGeneticsScanner = распечатка генетики
    .desc = Информация, распечатанная на ручном сканере генома.
