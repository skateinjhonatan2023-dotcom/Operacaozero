# Operação Zero · Costa de Ferro

Jogo 3D original, criado em Three.js para navegador e Android. O modo solo funciona offline com 12 bots. O mapa é um terminal costeiro com galpões acessíveis, contêineres, áreas abertas e cobertura. As partidas têm até 2 min 45 s, zona que contrai, suprimentos, cura, recarga, minimapa e tela de resultado.

A pesquisa usou a [descrição oficial de Free Fire](https://ff.garena.com/en?code=189), as [notas da Garena sobre o ritmo da zona](https://dl.dir.freefiremobile.com/common/web_event/aswqooiwd/c4e3de9f.html?lang=en) e [demonstrações públicas de Super Hero Royale](https://www.youtube.com/watch?v=ztcHIQ3j8wo). O mapa, os controles e a interface foram implementados nesta edição. Os recursos 3D e o áudio têm créditos em [CREDITOS.txt](CREDITOS.txt).

## Builds

A build web inclui os modelos, texturas, áudio, biblioteca e jogo em um HTML. Ela exige WebGL 2. O APK de desenvolvimento inclui o mesmo HTML e usa o Android System WebView com aceleração gráfica, na horizontal. Funciona sem conexão e requer Android 8 ou superior, com WebView que ofereça WebGL 2.

Use Three.js 0.170.0, esbuild 0.24.2 e Firebase 11.10.0. A partir da raiz do projeto:

```sh
npm install --prefix /caminho/dependencias esbuild@0.24.2 three@0.170.0 firebase@11.10.0
python3 Tools/build_costa_de_ferro.py Costa-de-Ferro.html --node-modules /caminho/dependencias/node_modules
Tools/build_costa_android.sh Costa-de-Ferro.html /caminho/build-tools/34.0.0 /caminho/android.jar Costa-de-Ferro.apk
```

A build Android requer Java, SDK Build Tools 34 e uma plataforma Android 34. Em ambientes com JRE, `OZ_ECJ_JAR` pode apontar para o Eclipse Compiler for Java 3.39.0. A variável opcional `OZ_ANDROID_DEBUG_KEYSTORE` permite assinar revisões com a mesma chave de desenvolvimento.

## Validação da primeira build

- Chromium com WebGL 2 em renderização por software. O lobby e a cena carregaram os recursos locais. A partida começou com 13 combatentes. Passaram movimentação, coleta, disparo e recarga no desktop.
- Chromium com toque emulado em 915 × 412. Passaram início de partida, disparo e recarga.
- Em um teste de regras no Chromium com as chamadas de desenho suspensas após carregar a cena, verifiquei a saída da zona, a eliminação e o início de nova partida.
- A navegação e a partida testadas dispensaram requisições HTTP externas.
- APK compilado com Android SDK 34, alinhado e verificado com assinatura de desenvolvimento v2/v3. A atividade inicial, o DEX e o HTML incorporado foram inspecionados. O HTML dentro do APK é idêntico ao distribuído para navegador.
- A instalação, a execução em Android físico e as métricas de FPS, memória e temperatura no OPPO A06 estão pendentes. Esta edição oferece partidas solo offline.

## Versão 0.2

- Painel inicial com acesso à partida e aos ajustes. O menu de pausa permite retomar a partida ou voltar ao painel.
- Quatro perfis gráficos, limite de 30/60 FPS, sensibilidade e mira por toque ou pressão. A predefinição inicial é Média a 30 FPS.
- Editor para arrastar controles, redimensioná-los, ajustar opacidade e restaurar o padrão. As preferências ficam no dispositivo pelo armazenamento local.
- Comandos de salto, agachamento e corrida. No teclado, use Espaço, C e Shift. O combate distingue dispersão ao mirar, em movimento e agachado e mostra confirmação distinta para acerto na cabeça.
- Arena de 208 × 208 m com quatro galpões adicionais e seis edifícios externos, mais suprimentos, 16 bots, zona inicial de 94 m de raio e três fases de contração.
- APK com o mesmo identificador e certificado da 0.1. A versão 0.2 foi testada em navegador com toque emulado. A medição de FPS e o teste da nova versão no aparelho continuam pendentes.

## Versão 0.3

- Dois toques em **Agachar** em até 320 ms iniciam um deslize na direção do movimento. Em repouso, o deslize segue a mira. No teclado, use C duas vezes. Uma colisão encerra o deslize, e o comando fica indisponível no ar.
- A arma acompanha a mão direita da animação. A mão esquerda usa a empunhadura de apoio nas armas longas. Essa integração é aplicada ao personagem do jogador e aos bots.
- O Arsenal do painel inicial oferece 81 equipamentos em 11 categorias. Escolha duas armas e troque no botão do HUD ou na tecla Q. A troca conserva a munição de cada arma e o equipamento é lembrado no dispositivo. Os bots continuam usando rifle.
- As categorias incluem tiro automático, semiautomático, rajada, múltiplos projéteis, explosão, corpo a corpo e utilitários. Granadas e barreiras usam regras próprias do modo solo. Vários modelos compartilham geometria por categoria.
- Referências do catálogo: [página oficial da Garena](https://ff.garena.com/en/weapons/) e [lista consolidada Free Fire Jornal](https://freefirejornal.com/en/free-fire-weapons/). Os nomes, categorias e quantidades específicas de eventos podem variar. A implementação usa os recursos licenciados registrados em [CREDITOS.txt](CREDITOS.txt) e geometria criada para o projeto.

## Versão 0.4

- O terminal ganhou dez segmentos de muro baixo e oito carros estacionados. O cenário bloqueia movimento, tiro e linha de visão dos bots. Os carros servem de cobertura.
- O botão **Pular** transpõe obstáculos baixos e permite subir em objetos com uma superfície livre. As oito escadas externas levam aos telhados. O mesmo comando funciona com Espaço.
- Cada partida sorteia **20 itens iniciais**: sete armas, oito caixas de munição e cinco kits. Sete pontos ficam dentro de galpões, quatro nos telhados e nove nos pátios e arredores. Há um espaçamento mínimo no mesmo nível. As eliminações continuam gerando seu loot separado.
- Ao equipar uma arma do chão, o item do slot ativo fica no mapa com a munição que restava. O segundo slot permanece disponível. As caixas de munição abastecem a arma ativa e os kits respeitam o limite de cinco.
- O desenho da distribuição usou as [notas da Garena sobre áreas de loot](https://ff.garena.com/en/article/1673) e a [dispersão de arsenais pelo mapa](https://ff.garena.com/en/article/1456) como referências.
- Os testes de geometria verificaram as oito subidas, os 16 pontos dos bots e 60 sorteios de loot. O Chromium também testou a transposição, a subida, a coleta e a troca de armas. A instalação da 0.4 em Android físico continua pendente.

## Versão 0.5

- O painel inicial oferece salas solo de 25, 35 ou 45 combatentes. As vagas são preenchidas por 24, 34 ou 44 bots. Os pontos são sorteados em quatro anéis, com distância mínima entre bots e do jogador.
- A Costa de Ferro passou para 384 × 384 m de terreno. A expansão preserva o terminal central e acrescenta serras escaláveis, vales, bairros industriais e rotas até os galpões. A topografia aparece no minimapa e bloqueia tiros e visão.
- O mapa tem 16 galpões com escadas externas, mais prédios, árvores, rochas, carros estacionados e barreiras. Troncos e rochas em pontos selecionados dão cobertura. As posições de loot continuam espaçadas, com 36 itens distribuídos em interiores, coberturas e pátios.
- A zona começa com 178 m de raio e se contrai em quatro fases. A partida pode durar até seis minutos.
- Os perfis gráficos controlam a densidade de vegetação e a distância de renderização dos personagens. Os bots continuam simulados mesmo quando estão fora dessa distância.
- Verifiquei a geometria do terreno, 16 rotas de subida, 80 sorteios de 44 bots e 36 itens, e os limites de cada fase da zona. No Chromium, iniciei salas de 25 e 45 combatentes, conferi a zona e subi um morro pelos controles. O desempenho da sala cheia em Android físico continua pendente.
- Referência de escala e variedade de regiões: [mapas oficiais do Free Fire](https://ff.garena.com/en/maps/). Os setores e caminhos foram desenhados para o Costa de Ferro.

## Versão 0.6

- O painel **Operadores** exibe a prévia animada e salva a seleção no dispositivo. Vigia, Brasa e Íris têm materiais, acessórios e funções próprias no mesmo esqueleto humanoide licenciado.
- **Vigia** marca adversários próximos no minimapa durante 8 segundos. **Brasa** absorve até 34 pontos de dano de combate durante 7 segundos. **Íris** regenera até 30 de vida em 6 segundos dentro da zona segura. Cada habilidade tem recarga. Use o botão personalizável **Habilidade** ou F no teclado.
- Os operadores têm tecido com detalhes, visor colorido e acessórios vinculados à animação. O cenário usa mapas normais CC0 para concreto, asfalto, grama e rocha. As sombras são concentradas ao redor do jogador. O perfil Baixa desliga os mapas normais e as sombras.
- A seleção e as habilidades foram verificadas no Chromium com toque emulado. Os testes de regras conferiram a recarga, a duração, a absorção de dano e a condição de cura dentro da zona. Instalação e desempenho em Android físico não foram testados.
- A estrutura de habilidades ativas foi estudada a partir da [explicação do Google Play sobre personagens de Free Fire](https://play.google.com/store/apps/editorial?hl=en_IE&id=mc_editorialevergreen_post_install_free_fire_tc_char_ability_types_fcp). As habilidades, os valores e os personagens do Costa de Ferro são próprios do jogo.

## Versão 0.7

- Brasa e Íris ganharam rostos e cabelos 3D diferentes a partir do pacote CC0 de Quaternius. As cabeças acompanham a animação do esqueleto em combate, e Vigia mantém o capacete. O seletor tem câmera de corpo inteiro e aproximação.
- A Doca Sul ganhou três píeres, dois guindastes, um pátio de carga e embarcações decorativas. O contorno costeiro tem faixa de areia, água com ondas e espuma animadas, e os galpões selecionados receberam revestimento canelado.
- As referências visuais incluíram a apresentação oficial de [Bermuda Remastered](https://www.youtube.com/watch?v=a1-XRkruIxI), a [apresentação da Kassie](https://www.youtube.com/watch?v=Laon2MSZLjA) e capturas publicadas da [tela de personagens](https://www.oneesports.id/free-fire/cara-dapat-karakter-kassie-free-fire/) e do [mapa costeiro](https://esports.gg/br/news-br/free-fire/nova-atualizacao-do-free-fire-traz-areas-ineditas-para-o-mapa-bermuda/). Os recursos que entram no jogo e suas licenças estão em [CREDITOS.txt](CREDITOS.txt).
- Testes de geometria conferiram as 16 escadas, 44 pontos de bots por partida e a distribuição do loot após a mudança de cenário. Os testes do navegador conferiram os três operadores, a seleção, a abertura da partida e a habilidade do Brasa.

## Versão 0.8

- A empunhadura usa a animação do ombro e do cotovelo como ponto de partida. Corrige a mão de apoio dentro do alcance do braço e restaura a pose original antes do próximo quadro. O seletor de operadores usa uma pose relaxada. Brasa ganhou placas presas ao colete e o equipamento do torso recebeu novos painéis e alças.
- Os 81 equipamentos têm acabamentos e perfis de som individuais. O M4A1 conserva o áudio CC0 dos créditos. Os demais disparos são sintetizados durante o jogo.
- O mapa sorteia duas granadas de fragmentação e duas de fumaça dentro dos 36 itens iniciais. O botão **Arremessar** e a tecla G usam o item selecionado. Use o seletor no HUD ou T para alternar. A explosão tem dano por distância e sofre bloqueio pela cobertura. A fumaça bloqueia a visão dos bots por oito segundos.
- A entrada como convidado funciona offline. Loja, inventário, caixas e roleta usam créditos das partidas e salvam os itens no dispositivo. A roleta e as caixas sorteiam com chances iguais entre os itens restantes. Trajes e pinturas equipados aparecem no personagem. O fluxo foi estudado a partir das apresentações oficiais de [lobby](https://www.youtube.com/watch?v=eMfLp0wGBz0) e [lobby de grupo](https://www.youtube.com/watch?v=LOUytNx2JiE) da Garena.

### Ativar contas na edição web

A compilação padrão, incluindo o APK, oferece o modo convidado. Para habilitar Google, Facebook e e-mail no navegador, registre um app web em um projeto Firebase, ative os provedores em Authentication e autorize o domínio HTTPS do jogo. Configure também a aplicação Meta e o endereço de retorno do Firebase para o Facebook. Exporte somente a configuração pública do app web para um arquivo JSON. [Firebase para web](https://firebase.google.com/docs/auth/web/start) · [Google](https://firebase.google.com/docs/auth/web/google-signin) · [Facebook](https://firebase.google.com/docs/auth/web/facebook-login)

```sh
python3 Tools/build_costa_de_ferro.py Costa-de-Ferro.html \
    --node-modules /caminho/dependencias/node_modules --firebase-config /caminho/firebase-web.json
```

A configuração online e as contas em um dispositivo Android exigem integração com o serviço de produção. O login identifica a conta na edição web configurada. Inventário, créditos e progresso são locais em ambas as edições. Partidas multiplayer seguem pendentes.

### Testes da 0.8

- Chromium com WebGL 2 e toque emulado. Foram conferidos o modo convidado persistente, a compra e o equipamento de traje e pintura, a abertura de caixa, o giro de roleta e a persistência do inventário. A verificação contou 81 acabamentos e perfis de áudio distintos.
- Na sala simulada de 25 combatentes, foram conferidos a distribuição de 36 itens, a coleta e o arremesso das duas granadas, a fumaça ativa e a perda de vida de um bot perto da explosão. O teste também percorreu mira, empunhadura e disparos com M4A1 e MP40.
- O APK foi compilado com SDK 34 e tem o mesmo certificado de desenvolvimento da 0.7. O HTML renderizado no navegador é idêntico ao incluído no APK. A instalação e o desempenho em Android físico continuam pendentes.
- Uma build web com configuração de exemplo confirmou a inclusão dos campos públicos. Google, Facebook e e-mail exigem configuração de produção para teste com contas reais.

## Versão 0.9

- A partida começa em um ponto de embarque explorável, com bots visíveis e uma contagem de oito segundos. O avião cruza o mapa na diagonal em 15 segundos. A rota aparece no minimapa e o salto é automático se o jogador esperar até o fim do voo.
- Use **Saltar do avião** e depois **Abrir paraquedas**. No teclado, use Espaço para as duas ações. O analógico ou WASD direciona a descida. O paraquedas abre sozinho a 24 m da superfície. É possível pousar em telhados, e o tempo de combate começa no pouso.
- A zona segura abre com **214 m de raio** e começa a fechar após **55 segundos de combate**. As quatro contrações terminam em 6 m de raio.
- Quatro tirolesas ligam os morros às áreas mais baixas na Serra do Farol, no Morro da Vigia, no Morro do Aço e no Vale da Oficina. As linhas laranja no minimapa mostram as duas pontas. Aproxime-se de uma estação e use **Usar tirolesa** ou X. Use **Soltar cabo**, X ou Espaço para sair antes de chegar à outra estação.
- A aeronave, o paraquedas e as estações foram modelados para Costa de Ferro. As referências de entrada, escolha de pouso e deslocamento vieram do [vídeo oficial de pouso](https://www.youtube.com/watch?v=LOloi6IQclc) e das notas da Garena sobre [área de espera](https://ff.garena.com/en/article/1170/), [rota aérea](https://ff.garena.com/en-pk/article/1317/) e [tirolesas](https://ff.garena.com/en/article/1306/).

### Testes da 0.9

- Verificação de geometria com 1.604 amostras ao longo dos cabos, 21 combinações de pontos de salto e direções de descida, pouso em telhado e raios das quatro fases da zona.
- Chromium com toque emulado validou a sala de embarque, o salto, a abertura manual e automática do paraquedas, o controle lateral pelo analógico, o pouso e o combate. As quatro tirolesas foram percorridas nos dois sentidos e a saída antecipada foi verificada. Também foram testados o salto automático na sala de 45 combatentes, a pausa no avião e o retorno ao painel durante a descida.
- O APK foi compilado com Android SDK 34 e assinado com a mesma chave de desenvolvimento da 0.8. A instalação e o desempenho em Android físico continuam pendentes.

## Versão 0.10

- As árvores e as rochas usam instâncias agrupadas por região. Árvores distantes usam 2.127 triângulos em vez de 12.795. Os bots mantêm o esqueleto e usam uma malha de 3.839 triângulos em uma chamada de desenho. Os rifles dos bots e as armas no chão usam uma malha simplificada. Os 45 combatentes continuam simulados na sala cheia.
- Os quatro presets têm limites de resolução. Baixa e Média usam água simplificada e eliminam os mapas normais do cenário. A resolução de sombras acompanha o preset. As texturas de cenário foram recompactadas. Os arquivos originais permanecem disponíveis para a edição do projeto.
- Movimento, consulta de piso, tiros e cobertura usam o mesmo índice espacial. Paredes temporárias entram e saem do índice quando aparecem e desaparecem. A câmera de ombro se recolhe diante de obstáculos. A explosão considera distância em três dimensões e cobertura.
- É possível mirar arrastando o botão de tiro. O analógico tem uma zona morta central. Movimento mantido durante a descida continua ativo no pouso. Ações de disparo no fim da recarga são executadas quando a arma fica disponível. O HUD horizontal compacto deixa mais espaço para o analógico, a mira e o botão de agachar. [Referência de controle e recarga da Garena](https://ff.garena.com/en/article/1424/) · [Referência de combate](https://www.youtube.com/watch?v=nb7opERHPIQ)
- Para regenerar a geometria dos bots após atualizar o GLB original, execute `node Tools/build_costa_bot_mesh.cjs` com `meshoptimizer@0.24.0` no caminho de módulos. O APK usa o resultado incluído no projeto.

### Testes da 0.10

- Comparação entre 0.9 e 0.10 em Chromium/SwiftShader, janela de 760 × 352, preset Baixa e sala de 45 combatentes. No início do combate, a mediana de triângulos por quadro passou de 642.630 para 183.916. A amostra de quatro segundos passou de 4 para 20 quadros renderizados. Esses números comparam as duas builds com renderização por software.
- O índice foi comparado à verificação exaustiva em 24.000 pontos de piso e movimento e 8.000 raios. Também foram testadas a criação e a remoção de parede, a geometria das quatro tirolesas e 21 combinações de descida com pouso, incluindo telhado.
- Os quatro presets foram renderizados na sala de 45 combatentes. O Chromium com toque emulado conferiu mira pelo tiro, disparo após recarga, troca de arma, salto, agachamento, deslize, colisão da câmera, bloqueio de explosão, pousos manual e automático, uso dos cabos nos dois sentidos e retorno ao lobby.
- O HTML final foi testado sem instrumentação e sem requisições externas. Foram confirmados o embarque, a abertura automática do paraquedas, o disparo após pousar e a separação dos controles e indicadores em 760 × 352 e 640 × 320.
- O APK usa a mesma assinatura de desenvolvimento da 0.9. Ainda não houve medição de FPS, memória ou temperatura em um Android físico.

## Versão 0.11

- A entrada apresenta uma ilustração própria da Costa de Ferro, acesso direto como convidado e opções de conta em um grupo expansível. O APK inicia como convidado offline, e os provedores da edição web continuam dependendo da configuração Firebase.
- O lobby tem um hangar 3D próprio, operador animado, atalhos de equipamento e recompensas, saldo e um painel separado para selecionar o tamanho da sala e entrar na partida. A câmera aproxima o operador ao inspecioná-lo. A apresentação no hangar usa resolução própria, separada dos limites selecionados para a arena.
- A arena recebeu novo céu, iluminação de fim de tarde, acabamento com desgaste no pé das fachadas, placas nos galpões, molduras de janelas, marcações viárias e variação de cor da água perto da margem. Os detalhes dos galpões entram nos lotes de renderização do cenário.
- A organização de atalhos e a prévia 3D foram estudadas nas notas da Garena. A identidade visual, a disposição dos elementos e a arte são da Operação Zero. [Atalhos do lobby](https://ff.garena.com/en/article/1170/) · [Lobby 3D](https://ff.garena.com/en/article/1683/)

### Testes da 0.11

- Chromium com toque emulado conferiu entrada e saída como convidado, disponibilidade dos provedores, seleção e inspeção de operador, arsenal e as quatro áreas de itens. A navegação e o botão de partida couberam em 640 × 320 e 390 × 780. Também foram testados embarque, pouso, disparo e retorno ao hangar sem requisições externas.
- No cenário comparativo em Chromium com preset Baixo, a cena de lobby passou de cerca de 128 mil para 12 mil triângulos por quadro. A geometria no início do combate permaneceu próxima de 185 mil triângulos no cenário medido. São contagens de renderização em navegador, e ainda não há medição de FPS da 0.11 em Android físico.
- As verificações de colisão, tirolesas e pousos foram repetidas depois das mudanças de cenário. O APK foi alinhado e verificado com a mesma assinatura de desenvolvimento da 0.10.

## Versão 0.12

- Os quatro presets usam suavização de bordas e nitidez adaptada ao contraste quando o WebGL 2 oferece renderização em ponto flutuante. Em dispositivos sem esse recurso a cena sai diretamente para a tela. A câmera usa interpolação baseada no tempo durante movimento, descida e tirolesa.
- A **Resolução dinâmica** fica ativa por padrão e pode ser desligada nas configurações. Ela reduz e recupera a resolução dentro dos limites do preset conforme o ritmo de renderização. A resolução interna aparece nas configurações.
- **Baixo** usa sombra de contato e terreno com variação de cor e acostamentos. **Médio** ativa mapas de relevo e sombras dinâmicas. **Alto** e **Ultra** carregam texturas difusas de 2K para as principais superfícies, mapas normais de 1K e detalhe de tecido. O Ultra tem limite de resolução, filtragem e sombras maiores. As texturas HD são liberadas ao retornar a um preset que dispensa esses recursos. As texturas continuam sob as atribuições registradas nos créditos.

### Testes da 0.12

- Os quatro presets renderizaram na mesma cena com 45 combatentes em Chromium/SwiftShader. Foram conferidos o filtro de bordas, a resolução de cada preset, a troca das texturas e o caminho de renderização direta com os buffers de ponto flutuante desabilitados.
- O controlador de resolução foi testado com intervalos sintéticos para verificar o piso, a recuperação e a pausa. Na comparação em software, as duas versões renderizaram 27 quadros em 4,8 s no preset Baixo, com 44 bots ativos. Na 0.12, a resolução interna caiu de 532 × 246 para 404 × 187 durante a amostra, com saída de 616 × 285 e suavização ativa.
- No HTML final, embarque, abertura automática do paraquedas e disparo rodaram sem requisições externas. As verificações de sobreposição do HUD passaram a 760 × 352 e 640 × 320.
- O APK foi alinhado e assinado com o mesmo certificado da 0.11. Os resultados acima são de navegador emulado. A estabilidade e o FPS em celulares Android físicos ainda não foram medidos.
