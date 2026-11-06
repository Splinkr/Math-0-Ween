# Validação e distribuição

Registro de preparação do portfólio em **06/11/2026**. Integridade de arquivos, resultado de compilação e observação de uma execução são evidências diferentes.

## Evidências disponíveis

| Item | Situação |
| --- | --- |
| Compilação de distribuição Windows x64/Mono | Relatório existente aprovado, com quatro verificações e nenhuma falha/erro |
| Compilação auxiliar com perfil de dados isolado | Relatório existente aprovado |
| Integridade da pasta de distribuição | 182 arquivos comparados com o inventário SHA-256 da build, sem ausências ou divergências |
| Formato do executável | Cabeçalho PE identificado como x64 |
| Regressão anterior | A documentação técnica registra 11.518 verificações aprovadas; não reexecutadas nesta preparação |
| Testes manuais Windows | Relatados pelo desenvolvedor, sem checklist, duração ou hash do executável associado |
| Smoke específico dos players gerados | Manifesto de aprovação ausente |
| ZIP e execução depois de extraído | Pendentes |
| Publicação na internet | Não realizada nesta preparação |

A build existente contém o aviso histórico de componente de script ausente em GameMaster. Nenhuma correção de gameplay foi realizada para preparar este repositório. Não há declaração de versão plenamente validada, benchmark do player, avaliação auditiva concluída ou certificação em outro computador.

## Condições para liberar o pacote

1. Confirmar as licenças dos recursos efetivamente incorporados, inclusive áudio, fontes, ícones e elementos de Resources; preparar os créditos necessários.
2. Conferir metadados de diagnóstico e de testes presentes no inventário de Resources antes de qualquer distribuição.
3. Registrar a inicialização do player final e o encerramento pela interface.
4. Validar salvamento e leitura em dois processos do player auxiliar, sem gravar no perfil real.
5. Confirmar a preservação do ranking real e de seu backup.
6. Montar uma cópia separada da build completa, excluindo depuração, logs e diretórios marcados DoNotShip.
7. Criar o ZIP fora do histórico Git, comparar inventários SHA-256 depois da extração e abrir o executável extraído.
8. Obter autorização explícita para publicação do repositório e, separadamente, da Release.

O nome reservado para esse futuro pacote é Math-0-Ween-v1.0.0-Windows-x64.zip. Não foi criado nesta preparação.

## Screenshots

Existem imagens reais de estados preparados no Unity para validação visual. Elas não comprovam partidas contínuas. A publicação das capturas depende de conferir as permissões dos recursos visíveis e os dados apresentados. Nenhuma imagem acompanha esta entrega.

Veja a [lista de capturas pendentes](../screenshots/README.md).

## Licenciamento

A declaração do desenvolvedor permite considerar o uso das texturas dentro do executável como informação fornecida pelo titular do projeto; ela não autoriza a entrega dos arquivos individuais ou, automaticamente, imagens de divulgação.

A origem e os termos das músicas ainda precisam ser documentados. A descrição de um download como gratuito, isoladamente, não comprova o direito de distribuir o áudio incorporado ao jogo. O [registro de terceiros](../THIRD_PARTY_NOTICES.md) distingue os componentes incluídos neste repositório dos recursos ainda pendentes da distribuição.
