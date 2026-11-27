# THIRD_PARTY_ASSETS — V0.7

Inventário original detalhado: [LICENCAS-ASSETS.md](LICENCAS-ASSETS.md), [Tools/assets.json](Tools/assets.json) e [créditos do lobby](Assets/Content/Lobby/Creditos.txt). Nenhum binário externo novo foi importado nesta etapa. Reutilização efetiva no código do piso: concrete.jpg e concrete-normal.jpg, da Poly Haven (Concrete Wall 008, CC0 conforme inventário anterior), em Assets/Content/Raw. Vigia (BAMEN, CC BY 4.0), fallback e animações Quaternius (CC0), rifle nisu (CC0), Barlow (OFL) mantêm as atribuições originais.

## Fontes consultadas e candidatos NÃO importados
- Poly Haven, CC0: https://polyhaven.com/license — Hangar Concrete Floor https://polyhaven.com/a/hangar_concrete_floor (diffuse, normal GL e roughness 1K pendentes).
- ambientCG, CC0: https://docs.ambientcg.com/license/ — materiais industriais, seleção individual pendente.
- Mixamo, Adobe: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html — uso em jogos conforme termos, download e conta não realizados; não publicar arquivos brutos sem permissão.
- Unity Asset Store: https://unity.com/legal/as-terms — verificar EULA por produto; não redistribuir arquivo bruto sem licença.
- Sketchfab, BAMEN: https://sketchfab.com/3d-models/free-military-soldier-female-warrior-rigged-ba5ad50a7a2f457a8b5ed7a84328027f (CC Attribution indicada na página); arquivo FBX/GLB, texturas e licença individual ainda precisam ser obtidos e validados. Nenhum login/download foi tentado. Não afirmar integração.

Não extrair recursos proprietários de jogos de referência.

## Pesquisa adicional — candidatos não importados
- RedVeil studio (matheofrelin3), AMERICAN SOLDIER RIGGED, CC Attribution indicada: https://sketchfab.com/3d-models/american-soldier-rigged-5b1967e7f83547be8a1c3ae656ab1ad4 — download, verificação do autor, texturas e rig pendentes.
- ambientCG, Metal 052 C (CC0): https://ambientcg.com/view?id=Metal052C — download e importação pendentes.
- Poly Haven, Hangar Interior (CC0): https://polyhaven.com/a/hangar_interior — HDRI candidato, não importado.
- Quaternius, Survival Pack (CC0): https://quaternius.com/packs/survival.html — seleção e importação pendentes.
A segunda etapa reutiliza os recursos já versionados e gera geometria original via código. Nenhum novo binário de terceiro foi adicionado.
## Integração de Nova Esperança e HUD

As atribuições desta revisão também acompanham o aplicativo em [Creditos-Ilha-Zero.txt](Assets/StreamingAssets/Creditos-Ilha-Zero.txt).

| Recurso | Autor e licença | Uso |
| --- | --- | --- |
| [Modular Urban Apartments Facade](https://polyhaven.com/a/modular_urban_apartments_facade) | James Ray Cock, CC0 | Mercado explorável, duas malhas visuais, malha de colisão e texturas locais de 512 px. |
| [Island Tree 01](https://polyhaven.com/a/island_tree_01) | Rob Tuytel e Rico Cilliers, CC0 | Dois níveis de detalhe, tronco físico e folhagem com recorte alfa. |
| [Wooden Crate 02](https://polyhaven.com/a/wooden_crate_02) | James Ray Cock e Jurita Burger, CC0 | Cobertura e decoração, conversão para FBX e material compartilhado. |
| [Game-icons.net](https://github.com/game-icons/icons/tree/82d948812bfe3f269ef8f731dcdb07b08160edc4) | Lorc e Delapouite, CC BY 3.0 | Sete pictogramas rasterizados. A lista individual está nos créditos distribuídos. |

## V0.8 — Operador de campo e solo

O FBX deriva de `WebGame/assets/soldier.glb`. A malha de BAMEN e as capturas 100STYLE foram adaptadas por cktang88 sob CC BY 4.0, com animações complementares de Quaternius sob CC0. A versão de campo inclui dois níveis de detalhe, 16 tomadas e materiais de uniforme do projeto. O piso de terra combina Grass Ground e Coast Rocks 01 da Poly Haven sob CC0. [Autores, fontes e licenças distribuídos com o jogo](Assets/StreamingAssets/Creditos-Ilha-Zero.txt).
