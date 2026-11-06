# Execução no Windows

**Situação atual:** a distribuição pública do Math-0-Ween ainda está pendente. Não há ZIP aprovado ou link de download nesta entrega.

## Quando houver uma distribuição autorizada

1. Obtenha o pacote completo Math-0-Ween-v1.0.0-Windows-x64.zip na Release oficial que vier a ser publicada.
2. Extraia todo o conteúdo para uma pasta local.
3. Abra MathFall.exe.
4. Mantenha MathFall_Data, UnityPlayer.dll, MonoBleedingEdge e todas as demais dependências junto ao executável. O .exe isolado não funciona.
5. Escolha a série no menu. Ao terminar, use os botões do jogo para sair.

O nome interno do executável e das pastas foi preservado. O produto apresentado ao público se chama **Math-0-Ween**; o nome do pacote pode seguir essa identidade sem renomear componentes da build.

O player foi gerado com Unity 6000.0.41f1, backend Mono, destino Windows x64 e sem Development Build. A configuração padrão é 1920×1080 em tela cheia, com janela não redimensionável. Não foram certificados requisitos mínimos ou desempenho em outros computadores.

Para uma inspeção em janela, os argumentos usados no procedimento técnico são:

~~~powershell
.\MathFall.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720
~~~

## Entrada e navegação

Digite os dígitos da resposta e confirme com Enter ou Espaço. Backspace apaga. Esc/P pausa ou inicia a retomada; X ativa Freeze e C inverte a gravidade quando disponíveis. Z usa o ataque coletivo com carga suficiente.

O menu permite escolher a série e abrir o Tutorial. Depois de Game Over, a interface de resultado permite confirmar o nome e salvar o ranking local. O Tutorial apresenta exemplos estáticos e não constitui uma partida com resultado salvo.

## Ranking e atualização

O jogo usa Application.persistentDataPath/scoreboard.json. Com a identidade interna atual do player Windows, o caminho é:

~~~text
%USERPROFILE%\AppData\LocalLow\DefaultCompany\MathFall\scoreboard.json
~~~

Esse é um caminho padrão por usuário, não um arquivo incluído na distribuição. O salvamento utiliza arquivo temporário e backup scoreboard.json.bak quando substitui um ranking existente. Dados inválidos bloqueiam a sobrescrita.

Para atualizar, feche o jogo e extraia o pacote completo em outra pasta. Preserve o ranking e seu backup antes de qualquer alteração manual. Não é necessário abrir o Editor Unity.

## Pendências antes do download público

Além das permissões de uso dos recursos incorporados, é preciso registrar inicialização do player final, persistência em dois processos com perfil isolado, preservação do ranking real e integridade/inicialização após extração do ZIP.

Consulte [validação e distribuição](VALIDACAO_E_DISTRIBUICAO.md) e [avisos de terceiros](../THIRD_PARTY_NOTICES.md).
