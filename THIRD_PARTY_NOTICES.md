# Créditos e avisos de terceiros

## Conteúdo efetivamente incluído neste repositório

Os arquivos de código selecionados conservam seu conteúdo original. Há uma adaptação identificada de código de terceiros:

| Componente | Autoria e origem | Permissão e arquivos |
| --- | --- | --- |
| PCG32 XSH-RR | Melissa O'Neill; adaptação C# de pcg-c-basic (2014) | Apache-2.0, limitada ao componente correspondente; avisos no código e licença completa ao lado do arquivo |

Copyright 2014 Melissa O'Neill. O cabeçalho de [Pcg32.cs](source/Assets/Script/Randomness/Pcg32.cs) identifica a adaptação e a origem. A [licença completa](source/Assets/Script/Randomness/LICENSE-PCG.txt) foi preservada. A referência upstream é [pcg_basic.c](https://github.com/imneme/pcg-c-basic/blob/master/pcg_basic.c).

Este repositório não concede uma licença open-source geral. A licença do PCG32 não deve ser interpretada como licença para os demais scripts, documentos, arte, áudio ou para o jogo completo.

## Recursos do jogo que não acompanham o repositório

Os itens abaixo foram identificados na auditoria do projeto ou no inventário da build. A menção é um registro de origem e pendência; seus arquivos não foram copiados. A distribuição Windows continua pendente de revisão completa.

| Recurso | Evidência e situação |
| --- | --- |
| Free Animated Explosion Sprite Pack, CraftPix | O arquivo local remete aos termos oficiais; estes distinguem uso incorporado em jogos da redistribuição dos arquivos de arte |
| Cartoon Button Pack II, Prinbles | Licença local permite uso pessoal/comercial e proíbe redistribuição do asset; arquivos originais excluídos |
| Teclas de Megacrash, paleta Journey | Readme local declara CC0 1.0; recursos não incluídos nesta seleção |
| Lucid Editor, Annulus Games | Licença MIT local identificada; ferramenta de Editor excluída |
| Autumn Forest 2D Pixel Art, Cainos, Health Bar e Pupkin warlock | Identificados no inventário da build; conservar comprovação da origem e dos termos aplicáveis |
| Recursos avulsos de ícones e imagens em Resources | Origem e permissões ainda precisam ser confirmadas |
| Crafted Odyssey - Cursed Chest e SimpleKeys | Identificados no projeto; não incluídos no repositório público; escopo de uso/licença a confirmar |
| Fontes e recursos Unity/TextMesh Pro/URP | Pacotes e arquivos não incluídos; reunir os avisos pertinentes antes de distribuir a build |

Referências: [termos CraftPix](https://craftpix.net/file-licenses/) e [página do Cartoon Button Pack II](https://prinbles.itch.io/cartoon-button-pack-ii). Não se estende a permissão de um fornecedor a outros recursos do projeto.

O desenvolvedor declarou que as texturas podem ser publicadas dentro do executável e não individualmente. Essa declaração não foi convertida em uma autorização geral para screenshots ou redistribuição de assets.

## Áudio identificado na build

| Identificação | Autoria/origem e situação |
| --- | --- |
| Dialogue, Pained Yelp, Loud, D.wav, ID 414070 | InspectorJ; página oficial indica CC BY 4.0 e exige atribuição |
| Sharp Explosion 2 of 5, ID 336005 | Arquivo identifica rudmer_rotteveel; termos específicos ainda a confirmar |
| Creepy e Halloween Time | Arquivos identificam Pecan Pie; confirmar a origem do download e licença compatível com distribuição em jogo |
| wed-may-21-2025_xEgIDPJK.mp3 | Origem, autoria e autorização pendentes |

Crédito identificado para o efeito de voz: **InspectorJ — Dialogue, Pained Yelp, Loud, D.wav**, disponibilizado no [Freesound](https://freesound.org/people/InspectorJ/sounds/414070/) sob [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Conferir e indicar eventuais modificações antes de distribuir o pacote.

O arquivo TunePocket-Halloween-Fun-Preview.mp3 existe no projeto, mas não aparece entre os áudios do inventário da build auditada. Sua presença no projeto não prova sua inclusão no player e não foi usada para atribuir uma licença à distribuição.

## Fontes e imagens de divulgação

O inventário da build identifica Exo-Bold.otf, LiberationSans.ttf e PerfectDOSVGA437.ttf; também há outros recursos de fonte no projeto. É necessário verificar os avisos aplicáveis às fontes e aos atlas/materializações incorporados, sem deduzir a permissão pelo nome do arquivo.

Nenhuma screenshot foi incluída. Os recursos visíveis em futuras capturas precisam de autorização de divulgação compatível com esse uso.

Consulte [validação e distribuição](docs/VALIDACAO_E_DISTRIBUICAO.md) para os demais requisitos de liberação.
