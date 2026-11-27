entity-effect-guidebook-speak = speaks a random phrase
entity-effect-guidebook-increases-reach = increases reach
entity-effect-guidebook-decreases-reach = decreases reach

entity-effect-guidebook-attack-others = {$chance ->
    [1] attacks
    *[other] attack
} a random mob nearby{$useHeld ->
    [true] {" "}with held item
    *[other] {""}
}

entity-effect-guidebook-attack-self = {$chance ->
    [1] attacks
    *[other] attack
} itself{$useHeld ->
    [true] {" "}with held item
    *[other] {""}
}

entity-effect-guidebook-delete-entity = {$chance ->
    [1] deletes
    *[other] delete
} the target entity

entity-effect-guidebook-nothing = {$chance ->
    [1] does
    *[other] do
} nothing

entity-effect-guidebook-relay-mutated = {$chance ->
    [1] for
    *[other] for
} the mutated mob, {$effect}

entity-effect-guidebook-scale-entity = {$chance ->
    [1] scales
    *[other] scale
} the target by ({$x}, {$y})

entity-effect-guidebook-set-standing = {$chance ->
    [1] makes
    *[other] make
} the target {$standing ->
    [true] stand
    *[other] fall
}

entity-effect-guidebook-start-use-delay = {$chance ->
    [1] triggers
    *[other] trigger
} cooldown for {$id}

entity-effect-guidebook-scramble-dna = {$chance ->
    [1] scrambles
    *[other] scramble
} the target's DNA
