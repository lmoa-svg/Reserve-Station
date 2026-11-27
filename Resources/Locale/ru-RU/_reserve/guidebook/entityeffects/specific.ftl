entity-effect-guidebook-remove-reactive = предотвращает реакцию цели с жидкостями
entity-effect-guidebook-add-trash-tag = делает цель перерабатываемой

entity-effect-guidebook-speak = произносит случайную фразу
entity-effect-guidebook-increases-reach = увеличивает дальность
entity-effect-guidebook-decreases-reach = уменьшает дальность

entity-effect-guidebook-attack-others = {$chance ->
    [1] атакует
    *[other] атакует
} случайное существо поблизости{$useHeld ->
    [true] {" "}удерживаемым предметом
    *[other] {""}
}

entity-effect-guidebook-attack-self = {$chance ->
    [1] атакует
    *[other] атакует
} себя{$useHeld ->
    [true] {" "}удерживаемым предметом
    *[other] {""}
}

entity-effect-guidebook-delete-entity = {$chance ->
    [1] удаляет
    *[other] удаляет
} целевую сущность

entity-effect-guidebook-nothing = {$chance ->
    [1] ничего не делает
    *[other] ничего не делают
}

entity-effect-guidebook-relay-mutated = {$chance ->
    [1] для
    *[other] для
} мутировавшего существа, {$effect}

entity-effect-guidebook-scale-entity = {$chance ->
    [1] масштабирует
    *[other] масштабирует
} цель в ({$x}, {$y})

entity-effect-guidebook-set-standing = {$chance ->
    [1] заставляет
    *[other] заставляет
} цель {$standing ->
    [true] встать
    *[other] упасть
}

entity-effect-guidebook-start-use-delay = {$chance ->
    [1] запускает
    *[other] запускает
} перезарядку для {$id}

entity-effect-guidebook-scramble-dna = {$chance ->
    [1] перемешивает
    *[other] перемешивает
} ДНК цели
