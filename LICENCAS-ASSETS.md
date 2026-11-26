# Licenças dos recursos incorporados

Os recursos abaixo usam [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/). Os arquivos distribuídos e seus hashes SHA-256 estão em `Tools/assets.json`.

| Conteúdo | Autor ou distribuidor | Origem e tratamento |
| --- | --- | --- |
| Personagem humanoide FBX, textura e mapa normal de corpo, textura de olhos | Quaternius | Edição Standard de [Universal Base Characters](https://quaternius.com/packs/universalbasecharacters.html). Texto da licença incluído em `Assets/Content/Quaternius/License-Characters.txt`. |
| Animações FBX in-place, edição Standard | Quaternius | [Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html). Texto da licença incluído em `Assets/Content/Quaternius/License-Animations.txt`. |
| Rifle, malha e mapas PBR | nisu | [M4A1 Assault Rifle, OpenGameArt](https://opengameart.org/content/m4a1-assault-rifle). A variante mobile consolida 14 malhas em uma e usa o gatilho como pivô. O mapa combinado coloca o metal no canal R e a suavidade invertida da rugosidade no alfa. |
| Rochas costeiras e texturas | Poly Haven | [Coast Rocks 01](https://polyhaven.com/a/coast_rocks_01). Convertido de glTF para FBX com LODs de 4.500 e 948 triângulos. |
| Texturas 2K de piso e construção | Poly Haven | [Asphalt 02](https://polyhaven.com/a/asphalt_02), [Concrete Wall 008](https://polyhaven.com/a/concrete_wall_008) e [Grass Ground](https://polyhaven.com/a/grass_ground). Importação Android configurada para ASTC com mipmaps. |
| Som de disparo | kurt | [Gunshots, OpenGameArt](https://opengameart.org/content/gunshots). Arquivo `22 Pistol.wav` convertido para mono a 44,1 kHz. |

A copa do paraquedas, o terreno, os armazéns, o loot e os efeitos de zona e traçantes são gerados pelo código. Os créditos do cliente web legado permanecem em `LegacyServer/CREDITOS.txt`.


## Lobby Unity V0.6

| Conteúdo | Origem e licença |
| --- | --- |
| `Vigia.fbx` | Modelo de BAMEN, [FREE Military Soldier RIGGED](https://sketchfab.com/3d-models/free-military-soldier-rigged-e9c56308a67d4a3db62e914fafa4d198), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Derivado do GLB adaptado por [cktang88](https://github.com/cktang88/3d-fps/blob/main/README.md#credits--licences), com o ciclo `ready_idle`, materiais alterados e posicionamento do rifle gravado na animação. Créditos de origem incluem [Quaternius](https://quaternius.com/) e [100STYLE](https://zenodo.org/records/8127870) de Ian Mason, Sebastian Starke e Taku Komura. O rifle de nisu mantém CC0. |
| Barlow Condensed | [The Barlow Project Authors](https://github.com/jpt/barlow), SIL OFL 1.1. Pesos regular, negrito e extra-negrito itálico convertidos de WOFF para TTF. Licença acompanhada no projeto e acessível no lobby. |
| Cenário e cartões | Panorama original gerado para Operação Zero. Os cartões de operador e rifle derivam de renderizações dos modelos creditados e o cartão de pacotes deriva do panorama. |
| Marca, painéis e tecido | Grafismos, textura de tecido e normal criados para Operação Zero. |

## Ambiente nativo da V0.6

O HDRI Kloofendal 48d Partly Cloudy (Pure Sky) é de Greg Zaal, com edição do céu por Jarod Guest, sob CC0. O projeto inclui o HDR de 1K e o importa como cubemap para o céu e os reflexos. [Fonte e licença](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky)

Os mapas normais OpenGL de 1K vêm das mesmas coleções da Poly Haven usadas nas superfícies difusas. O importador marca os três como mapas normais e configura ASTC para Android. Os arquivos originais e os endereços individuais constam no manifesto de conteúdo. [Asphalt 02](https://polyhaven.com/a/asphalt_02) · [Concrete Wall 008](https://polyhaven.com/a/concrete_wall_008) · [Grass Ground](https://polyhaven.com/a/grass_ground)
