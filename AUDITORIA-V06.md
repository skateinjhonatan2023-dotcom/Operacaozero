# Operation Zero V0.6 · auditoria e consolidação da Unity

A aprovação da V0.6 depende de uma compilação Unity ARM64/IL2CPP, da inspeção de lobby e partida no executável e de testes em Android físico.

## Inventário e origem das versões

Foram examinados o histórico local desde o ZIP REV07, as cinco branches de desenvolvimento anteriores, os arquivos rastreados, os dois remotos e os arquivos APK em attachments, shared e oz-download. A branch de publicação e a branch do lobby continham o mesmo projeto Unity. A branch de publicação incluía também o histórico do main de Operacaozero. Ela serviu de base à nova branch. O cliente web desta branch foi atualizado com os arquivos da 0.14 já testada.

| Caminho | Evidência | Situação |
| --- | --- | --- |
| Assets, Packages e ProjectSettings | Unity 6000.0.38f1, URP 17.0.3, uGUI, gerador de cena em C# | Cena construída no Editor. Ator armado, câmera, modos offline, bots, zona, loot e lobby têm implementação nativa. Importação e execução na Unity pendentes nesta revisão. |
| WebGame | GLB local de soldado com esqueleto, operadores e rifle, Three.js e atividade Android WebView | Os 17 arquivos APK com HTML encontrados incluem cópias e versões anteriores. O histórico chega à Costa de Ferro 0.14. A perda do WebGL 2 impede a inicialização deste cliente. |
| APK anexado OperacaoZero-v0.6-Android | Biblioteca libgodot_android.so para ARM64 | Artefato Godot. A busca pelos APKs locais não encontrou um player Unity ARM64/IL2CPP. |
| LegacyServer | API aiohttp, WebSockets, SQLite e cliente de navegador próprio | O adaptador gráfico opcional depende da importação de Three.js e do carregamento de modelos. Uma exceção mantém o renderizador básico. A falha ao inicializar o WebGL do renderizador básico aciona desenho por software. |
| Histórico remoto | Consulta ao destino com hífen retornou 404 no conector e 403 via Git. O destino público autorizado Operacaozero expôs main em d67a2b7 | O teste de escrita via Git e a tentativa de gravar um objeto pelo conector retornaram HTTP 403. |

## Causas da diferença visual

O lobby usa Vigia.fbx e um panorama de fundo. A partida usa outro FBX humanoide de Quaternius. Quatro perfis de arma compartilham o M4A1 e o áudio de disparo. Parte dos clipes é de pistola. A ilha gerada usa duas construções modulares simples, barreiras e rochas. Os pisos e as paredes recebiam UVs de uma única face de cubo esticados até a dimensão do objeto. Os perfis gráficos ajustavam escala e alcance das sombras. Os sinais no código explicam diferenças de conteúdo e de renderização. Para estabelecer causas de travamentos em um telefone específico ainda falta um perfil de CPU, GPU e memória.

## Implementação desta etapa

- O gerador configura URP em espaço de cor linear, disponibiliza dois renderizadores e inclui os recursos de pós-processamento. Alta e Ultra selecionam o renderizador com SSAO em meia resolução e o volume global de cor com tonemapping ACES. A preparação cria um material de céu a partir do HDRI da Poly Haven e usa o cubemap para reflexos.
- A grama, o asfalto e o concreto usam mapas normais locais. A geração da malha das construções dimensiona as UVs em metros e reutiliza malhas com as mesmas dimensões. As colisões continuam usando os volumes do gerador.
- Leve, Média, Alta e Ultra controlam resolução, distância de câmera, alcance e tamanho das sombras e MSAA. Um controlador reduz a resolução sob carga sustentada e aguarda estabilidade para aumentá-la. O lobby oferece a meta de 90 FPS em telas que informam pelo menos 89,5 Hz.
- A compilação prepara e reabre a cena salva, verifica URP, céu, reflexos, volume de cor, referências do lobby e malhas do operador e do rifle. Essa verificação roda dentro do Editor durante a compilação.
- O servidor voltou a iniciar em um checkout limpo. Serve os modelos e texturas locais do projeto quando a pasta opcional de recursos do servidor está ausente. As contas e os dados locais existentes continuam usando os formatos anteriores.

## Verificações executadas

| Teste | Resultado observado |
| --- | --- |
| python3 Tools/preflight.py | 44 arquivos conferidos por tamanho e SHA-256, incluindo os nomes dos 11 clipes exigidos no FBX. |
| HDRI e mapas normais | O HDR local coincidiu byte a byte com o download de origem. Os MD5 dos três JPEGs coincidiram com os mapas OpenGL 1K do catálogo da Poly Haven. |
| FBX importados no Blender | Vigia apresentou 78 ossos e 73 amostras de animação verificadas. O personagem de partida apresentou 65 ossos e três malhas com skin. A biblioteca apresentou 43 clipes. O M4A1 mobile apresentou 5.159 vértices e UVs. As rochas apresentaram LODs de 4.500 e 948 triângulos. |
| dotnet run --project Tools/GraphicsTests | O controlador C# passou nos quatro perfis e metas de 30, 60 e 90, com intervalos de quadro sintéticos. Foram conferidos limites, queda de escala, recuperação, pausa e preferências antigas. |
| Integração do servidor | As três suítes passaram com WebSockets separados. Cobriram cadastro, senha incorreta, entrada em sala, snapshots, eliminação, vitória, ranking, sessão duplicada, munição, recarga e rejeição de movimento irregular. As três rotas de recursos retornaram HTTP 200 e os bytes esperados. |
| Verificações de fonte e build | Análise sintática dos 19 scripts Unity em C#, validação do script shell e verificação de diferenças pelo Git. O comando de build parou na detecção do Editor. |
| Android | Unity Profiler, sessão prolongada, consumo de memória, temperatura, estabilidade de quadros, comparação visual e multitouch em aparelhos físicos pendentes. |

## Dependências e etapas em aberto

O catálogo oficial informa 8.550.010.880 bytes instalados para o Editor Linux 6000.0.38f1 e mais 1.330.635.624 bytes para o módulo Android, antes das dependências adicionais. O volume local tinha cerca de 2,8 GiB livres. O Editor e as credenciais de licença não estavam disponíveis nesta máquina. O SDK Android 34 usado na edição web está presente. [Versão oficial do Editor](https://unity.com/releases/editor/whats-new/6000.0.38f1)

Para compilar no ambiente com Unity, use o comando abaixo na raiz do projeto. Após a compilação, o resultado de desenvolvimento fica em Builds/OperationZero-debug.apk.

```sh
bash Tools/build_android.sh
```

Para testar o servidor com persistência, instale LegacyServer/requirements.txt e execute LegacyServer/server.py. PORT seleciona a porta, OZ_DB aponta para o SQLite e OZ_COUNTDOWN define a contagem da partida em segundos. Os padrões são 8080, LegacyServer/players.sqlite3 e 10 segundos. O cliente nativo usa o endereço HTTPS em Ajustes para a API de conta e ranking. As partidas Unity continuam locais.

A validação visual da etapa 1 precede a aprovação de dois operadores, armas por categoria e animações de combate. Continuam pendentes os interiores e a vegetação detalhada da Ilha Zero, controles multitoque validados em aparelho, novos sons, inventário completo, avião no modo nativo, melhorias de bots, multiplayer nativo com reconexão, recursos sociais e progressão completa. A UI de compra atual usa créditos de teste e inventário local. As imagens de conceito continuam sendo referências de direção artística.
