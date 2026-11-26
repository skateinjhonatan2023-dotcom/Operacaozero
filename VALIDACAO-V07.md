# OPERATION ZERO V0.7 — NEW GENERATION — validação preliminar

Status: desenvolvimento, não é release nem APK aprovado. Base solicitada: 8bb12f22c36dce04e0ab70a508e9b914cc09b07d. A branch codex/v06-motor-nativo permanece nesse commit. Build #6 (run 45894351579) reporta sucesso em a523ae9e4fccfa1f9a6fa850469a46d9c8220ec2; comparação reportou divergência. A revisão solicitada contém Unity 6000.0.38f1, não 6000.0.68f1. Não alterar Unity ou disparar build até reconciliar a base. Workflow Android e manifesto preservados.

## Alterações de código
- 79c37b5886c2c73df50ab0125ad2071bb18f4603: rotação por drag, reset, enquadramento, proteção da importação Vigia.
- a9dbc596ac81867cf3d017b22578e99fd1ee9b3f: orientação corporal relativa ao movimento, proteção de câmera e postura, ADS, ombro, proteção de clips.
- f5a657c8781d177ceb2aaba1251720b18c8646f6: multitouch e safe area, limpeza de toques, ocultação do corpo FPS (sombras), piso 3D com recursos existentes, validação do lobby na cena.
- Identificação V0.7 DEV e versionamento 0.7/7 neste commit; não constitui publicação.

## Verificações feitas
- OK (inspeção de fonte/API): branch V0.6 preservada
- OK (inspeção de fonte/API): branch V0.7 no commit esperado
- OK (inspeção de fonte/API): workflow preservado
- OK (inspeção de fonte/API): dependências preservadas
- OK (inspeção de fonte/API): versão Unity preservada
- OK (inspeção de fonte/API): drag integrado ao lobby
- OK (inspeção de fonte/API): ADS integrado
- OK (inspeção de fonte/API): ombro integrado
- OK (inspeção de fonte/API): fallback protegido
- OK (inspeção de fonte/API): validação na geração de cena
- OK (inspeção de fonte/API): safe area integrada
As verificações são de presença de integração textual e integridade de referências Git, não comprovam execução C#.

## Não executado
Não há Unity Editor, compilador C# ou Android físico neste ambiente. Não foram executados preflight de hashes binários, EditMode, PlayMode, compilação, medição de FPS, capturas reais, testes de servidor ou testes com dois clientes. Nenhum APK novo. Validar OZBuild.Prepare e OZBuild.ValidateSavedScene no Unity; verificar gestos, troca de perspectiva, colisões e postura, retorno ao lobby, fluxo offline. Não iniciar build antes das validações. A ferramenta de geração de cena é a existente (OZBuild), não uma cena nova comprovadamente salva.

## Pendências reais
Vigia permanece Generic com clipe original e fallback humanoide Quaternius; não há quatro operadores profissionais novos, conversão Humanoid do Vigia, IK completo, animações novas, armas com modelos exclusivos, lobby militar integral, música, região Ilha Zero 2x2 km, expansão, veículos, NavMesh, HUD configurável, multiplayer real, teste físico OPPO A06. Mapa atual 320x320 m; fundo panorâmico ainda presente. Recursos externos não importados; licenças em THIRD_PARTY_ASSETS.md e LICENCAS-ASSETS.md. Preservar modos existentes e priorizar teste do P0 antes de expandir.

## Atualização de escopo — segunda etapa
Commits: 88e2873e41a08e78f3c9d45758246083db8eca5e, 6cf54670a0e736f4b24e2545b93affd958df33c2, 2c40d6a391961724fc65d00050e050e927c753f8 e commit de documentação/polimento subsequente. As observações acima sobre mapa, bots e veículos descrevem o estado anterior. A nova implementação gera uma região de 2 × 2 km com dez distritos, portas, estradas, vegetação e veículos provisórios. Não é mapa final detalhado: não há streaming por setores, NavMesh, LOD para todos os objetos, rios, pontes ou interiores completos; colisões, posicionamento e performance não foram testados. Até 20 participantes offline, zona redimensionada, suprimentos periódicos (sem avião/carga visual), IA por decisões e raycasts (não NavMesh), treinamento passivo, veículos simplificados (sem modelos finais, sons ou animação de entrada), granada e escopeta de código com arma visual reaproveitada. HUD continua IMGUI; não há editor de layout ou inventário visual completo. Não há quatro novos operadores profissionais, novos clips, prone, IK integral, multiplayer real, ranking validado, captura ou APK novo. Nenhuma execução de testes nesta etapa, conforme instrução expressa do usuário. As onze verificações anteriores só se aplicam à primeira etapa. Não alterar workflow nem V0.6.