# MindComponent localization

comp-mind-ghosting-prevented = Вы не можете стать призраком в данный момент.

## Messages displayed when a body is examined and in a certain state

comp-mind-examined-catatonic = { CAPITALIZE(SUBJECT($ent)) } находится в глубоком сне.{ OBJECT($ent) } сон должно быть про рыбку аутиста...
comp-mind-examined-dead =
    { CAPITALIZE(SUBJECT($ent)) } { GENDER($ent) ->
        [male] мёртв
        [female] мертва
        [epicene] мертво
       *[neuter] мертвы
    }
comp-mind-examined-ssd = { CAPITALIZE(SUBJECT($ent)) } рассеяно смотрит в пустоту и ни на что не реагирует. { CAPITALIZE(SUBJECT($ent)) } может скоро придти в себя.
comp-mind-examined-dead-and-ssd = { CAPITALIZE(POSS-ADJ($ent)) } душа бездействует и может скоро вернуться.
comp-mind-examined-dead-and-irrecoverable = { CAPITALIZE(POSS-ADJ($ent)) } душа впала в глубокий сон.
