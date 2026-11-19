# -*- coding: utf-8 -*-
"""Tier 2 points of interest (Yan, 2026-11-19: "they do not need unique bosses in every dungeon, some can be a cool story, squads occupying the place, visual storytelling, random radiant quest targets"):
the papers of the Basin, the Scrapyard and Black Valley, in English and Portuguese (Brazil). One thread runs through them: a pulse that beats every eleven seconds somewhere to the north, dogs sitting facing it,
the Garrison taking people down the Loading Dock hatch, and the few who walked out and did not speak (the Husks).
python3 ArtSource/story/tier2_poi_docs.py  -> Assets/Scripts/GameCore/Tier2Poi.Docs.cs (English, read by Docs.Read) and the Portuguese lines appended to Assets/Resources/Lang/pt-BR.txt (once)."""
import os
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE))
# id: (title, [pages], journal, flag?)  -- every text is (en, pt)
D = {
 "bus": (("SURVEY FIELD LOG - DAY 2", "DIÁRIO DE CAMPO DA PESQUISA - DIA 2"), [
   ("The minibus broke its axle a kilometre short. Radio dead. Dr. Lenko says the pulse is strongest here, in the old yard: one beat every eleven seconds, like a heart. We measured it for six hours. The needle never moved a degree.",
    "O micro-ônibus quebrou o eixo a um quilômetro daqui. Rádio morto. O Dr. Lenko diz que o pulso é mais forte aqui, no pátio velho: uma batida a cada onze segundos, como um coração. Medimos por seis horas. A agulha nunca se mexeu um grau."),
   ("Day 3. Boris went to find the Cutting road and did not come back. The dogs - not dogs - circle the fence at night. Marta is crying and says she hears it too, in her teeth. We are staying in the bus.",
    "Dia 3. O Boris foi procurar a estrada do Cutting e não voltou. Os cães - que não são cães - rondam a cerca à noite. A Marta está chorando e diz que também ouve, nos dentes. Vamos ficar no ônibus."),
   ("Day 5. Whoever finds this: the pulse is not from the ground. It comes from the north, and it is not a machine.",
    "Dia 5. Quem encontrar isto: o pulso não vem do chão. Vem do norte, e não é uma máquina.")],
  ("The Rusted Bus: a Surveyor field party measured a pulse in the Scrapyard and died waiting for rescue. It beat every eleven seconds, came from the north, and 'is not a machine'.",
   "O Ônibus Enferrujado: uma equipe de campo dos Surveyors mediu um pulso no Scrapyard e morreu esperando resgate. Batia a cada onze segundos, vinha do norte e 'não é uma máquina'.")),
 "tally": (("WATCHER'S TALLY", "CONTAGEM DO VIGIA"), [
   ("Crane cab, day 61. I keep the tally because no one else will. The Garrison drag people to the Loading Dock in bags. Day 19: four in. Day 33: nine in. Day 47: fourteen in.",
    "Cabine do guindaste, dia 61. Faço a contagem porque ninguém mais faz. A Garrison arrasta gente para a Doca de Carga em sacos. Dia 19: quatro entraram. Dia 33: nove. Dia 47: catorze."),
   ("Three out, ever. They came up the hatch by themselves, at night, and walked south in a line, not speaking. I did not shoot. I should have.",
    "Três saíram, no total. Subiram pela escotilha sozinhos, à noite, e foram para o sul em fila, sem falar. Não atirei. Devia ter atirado."),
   ("I am leaving the rifle and the tally. If you are the next watcher, count carefully.",
    "Estou deixando o fuzil e a contagem. Se você é o próximo vigia, conte com cuidado.")],
  ("The Crane Cab: a watcher counted the prisoners the Garrison took into the Loading Dock hatch - and the three who came out by themselves, walking south in a line, silent.",
   "A Cabine do Guindaste: um vigia contou os prisioneiros que a Garrison levou pela escotilha da Doca de Carga - e os três que saíram sozinhos, indo para o sul em fila, em silêncio.")),
 "manifest": (("DISPOSAL MANIFEST 14", "MANIFESTO DE DESCARTE 14"), [
   ("Drums 1-40: solvent, routine. Drums 41-52: marked G, from the Institute lab at the Dock, sealed by hand. Do not open. Do not burn. Do not let the dogs near.",
    "Tambores 1-40: solvente, rotina. Tambores 41-52: marcados G, do laboratório do Instituto na Doca, lacrados à mão. Não abra. Não queime. Não deixe os cães chegarem perto."),
   ("(In pencil, below:) The dogs were already near.", "(A lápis, embaixo:) Os cães já estavam perto.")],
  ("The Chemical Dump: twelve drums marked G came from the Institute's lab under the Loading Dock, sealed by hand. Someone noted the dogs were already near.",
   "O Depósito Químico: doze tambores marcados G vieram do laboratório do Instituto sob a Doca de Carga, lacrados à mão. Alguém anotou que os cães já estavam perto.")),
 "transformer": (("TRANSFORMER LOG", "REGISTRO DO TRANSFORMADOR"), [
   ("T4 hums on the beat again. Eleven seconds, exactly. I replaced the coil twice. The hum comes back. Reported to the Plant.", "O T4 zumbe no ritmo de novo. Onze segundos, exatos. Troquei a bobina duas vezes. O zumbido volta. Avisei a Usina."),
   ("The Plant says ignore the hum. The Plant stopped answering on the third day.", "A Usina diz para ignorar o zumbido. A Usina parou de responder no terceiro dia.")],
  ("The Substation: transformer T4 hums on the same eleven-second beat. The Plant told the linemen to ignore it, then stopped answering.",
   "A Subestação: o transformador T4 zumbe no mesmo ritmo de onze segundos. A Usina mandou os eletricistas ignorarem, e depois parou de responder.")),
 "roster": (("NIGHT SHIFT ROSTER", "ESCALA DO TURNO DA NOITE"), [
   ("Night shift, Central Plant: fourteen names. Day 1 of the hum: fourteen on shift. Day 2: nine. Day 3: the roster ends.", "Turno da noite, Usina Central: catorze nomes. Dia 1 do zumbido: catorze no turno. Dia 2: nove. Dia 3: a escala acaba."),
   ("In the foreman's hand, in the margin: They did not leave. They are in the boiler hall. Do not go in after dark.", "Na letra do capataz, na margem: Eles não saíram. Estão no salão das caldeiras. Não entrem depois de escurecer.")],
  ("The Central Plant: a night-shift roster ends on the third day of 'the hum'. The foreman wrote that the missing men did not leave, and are in the boiler hall.",
   "A Usina Central: a escala do turno da noite acaba no terceiro dia do 'zumbido'. O capataz escreveu que os homens que sumiram não saíram, e estão no salão das caldeiras.")),
 "notice": (("EVACUATION ORDER", "ORDEM DE EVACUAÇÃO"), [
   ("To all residents of Block C: evacuation is ordered for 06:00. Bring your documents and one bag. Doors are to be left open so crews can check each flat.", "A todos os moradores do Bloco C: a evacuação está marcada para as 06:00. Levem os documentos e uma mala. As portas devem ficar abertas para que as equipes vistoriem cada apartamento."),
   ("The notice is not stuck to any door. All forty copies are stacked on the caretaker's desk, under a cold cup of tea.", "O aviso não está colado em porta nenhuma. As quarenta cópias estão empilhadas na mesa do zelador, sob uma xícara de chá frio.")],
  ("The Workers' Flats: an evacuation order for Block C was never delivered. The forty copies sit on the caretaker's desk beside a cold cup of tea.",
   "Os Apartamentos dos Operários: uma ordem de evacuação do Bloco C nunca foi entregue. As quarenta cópias estão na mesa do zelador, ao lado de uma xícara de chá frio.")),
 "ford": (("FORD CROSSING GAUGE", "MEDIDOR DA TRAVESSIA DO VAU"), [
   ("Water level 20 cm at 14:00. 140 cm at 15:10. No rain upstream.", "Nível da água: 20 cm às 14:00. 140 cm às 15:10. Sem chuva rio acima."),
   ("(Written later, shaking:) Upstream, a sound like a door closing. Then the water. The convoy is under there. We heard the trucks for a long time.", "(Escrito depois, com a mão tremendo:) Rio acima, um som como uma porta se fechando. Depois a água. O comboio está lá embaixo. Ouvimos os caminhões por muito tempo.")],
  ("The Ford: the water rose 120 cm in seventy minutes with no rain, after 'a sound like a door closing' upstream. A convoy is under it.",
   "O Vau: a água subiu 120 cm em setenta minutos sem chuva, depois de 'um som como uma porta se fechando' rio acima. Um comboio está debaixo dela.")),
 "tankorders": (("ORDERS, 6TH ARMOURED PICKET", "ORDENS, 6º PIQUETE BLINDADO"), [
   ("Hold the high ground over the Chokepoint. Relief by dawn.", "Manter a posição elevada sobre o Chokepoint. Rendição ao amanhecer."),
   ("(Scorched, in another hand:) No relief. We stopped receiving at 03:10. The commander ordered every hatch shut. At dawn he opened his. It was very quiet. He walked north without his rifle, and the crew followed him in a line.", "(Chamuscado, em outra letra:) Sem rendição. Paramos de receber às 03:10. O comandante mandou fechar todas as escotilhas. Ao amanhecer ele abriu a dele. Estava muito quieto. Foi para o norte sem o fuzil, e a tripulação o seguiu em fila.")],
  ("The Burnt Tank: a picket was ordered to hold the Chokepoint; at dawn its commander opened his hatch and walked north without his rifle, and the crew followed in a line.",
   "O Tanque Queimado: um piquete recebeu ordem de manter o Chokepoint; ao amanhecer o comandante abriu a escotilha e foi para o norte sem o fuzil, e a tripulação o seguiu em fila.")),
 "placard": (("THE PLACARD", "A PLACA"), [
   ("A board nailed to the trunk: THIEF. TOOK FROM THE SHARED CRATE. LET HIM HANG TILL HE DRIES. - the Raiders of the Maze.", "Uma tábua pregada no tronco: LADRÃO. PEGOU DA CAIXA COMUM. DEIXEM PENDURADO ATÉ SECAR. - os Raiders do Maze."),
   ("Scratched under it, much later, with a nail: The crate was empty when we got there. - Pyotr", "Riscado embaixo, muito depois, com um prego: A caixa já estava vazia quando chegamos. - Pyotr")],
  ("The Hanged Man's Tree: the Raiders of the Maze hanged a man for emptying the shared crate. Someone scratched under the placard that it was already empty.",
   "A Árvore do Enforcado: os Raiders do Maze enforcaram um homem por esvaziar a caixa comum. Alguém riscou sob a placa que ela já estava vazia.")),
 "waybill": (("WAYBILL", "GUIA DE REMESSA"), [
   ("40 crates, medical, cold chain, to the Bonfire camp. Driver: Fedir.", "40 caixas, material médico, cadeia de frio, para o acampamento Bonfire. Motorista: Fedir."),
   ("(Smeared, in a different pen:) Fedir saw it cross the road. It walked on two legs and had too many elbows. He swerved. The crates are still cold.", "(Borrado, com outra caneta:) O Fedir viu aquilo atravessar a estrada. Andava em duas pernas e tinha cotovelos demais. Ele desviou. As caixas ainda estão frias.")],
  ("The Overturned Lorry: a medical shipment crashed when the driver swerved from 'something on two legs with too many elbows'. A trail of blood leads away from it.",
   "O Caminhão Tombado: uma carga médica bateu quando o motorista desviou de 'algo em duas pernas com cotovelos demais'. Um rastro de sangue sai dali.")),
 "hunterlog": (("HUNTER'S LOG", "DIÁRIO DO CAÇADOR"), [
   ("Pallid dogs, fourteen this season. Blind Maws, three. Rats are not worth the lead.", "Cães pálidos, catorze nesta temporada. Blind Maws, três. Ratos não valem o chumbo."),
   ("Something new in the north wood: tall, antlers like a stag, walks on its hind legs. It stands at the edge of the clearing and the pack comes to it like it called them. I did not shoot. I wanted to see what it wanted. It pointed at me.", "Algo novo na mata do norte: alto, chifres como um cervo, anda nas patas de trás. Fica na beira da clareira e a matilha vai até ele como se o tivesse chamado. Não atirei. Queria ver o que ele queria. Ele apontou para mim."),
   ("Left the shack for a week. When I came back every dog in the valley was sitting facing north. I am going south. Take the rifle if you are staying.", "Saí da cabana por uma semana. Quando voltei, todos os cães do vale estavam sentados virados para o norte. Estou indo para o sul. Fique com o fuzil se for ficar.")],
  ("The Hunter's Shack: a hunter logged a tall antlered thing that stands at the clearing's edge and calls the pack, and pointed at him. Then every dog in the valley sat facing north.",
   "A Cabana do Caçador: um caçador registrou uma coisa alta e com chifres que fica na beira da clareira e chama a matilha, e que apontou para ele. Depois todos os cães do vale se sentaram virados para o norte.")),
 "minediary": (("MINER'S DIARY", "DIÁRIO DO MINEIRO"), [
   ("Gallery 4 broke through into something warm. The wall is warm, like a flank. The foreman says it is a hot spring. There are no springs on this ridge.", "A Galeria 4 rompeu em algo quente. A parede é quente, como um flanco. O capataz diz que é uma fonte termal. Não há fontes nesta serra."),
   ("We hear singing in the drift: one note, over and over. Old Pavlo says it is a pipe. There are no pipes.", "Ouvimos cantoria na galeria: uma nota só, sem parar. O velho Pavlo diz que é um cano. Não há canos."),
   ("We sealed the adit. We can still hear it through the rock. It has changed notes.", "Lacramos a entrada. Ainda ouvimos através da rocha. Mudou de nota."),
   ("If you are reading this we did not open the adit. Do not open it.", "Se você está lendo isto, nós não abrimos a entrada. Não a abra.")],
  ("The Old Mine Works: miners broke into a warm wall and heard singing - one note, then another - from the drift. They sealed the adit and left a warning not to open it.",
   "As Minas Velhas: mineiros romperam uma parede quente e ouviram cantoria - uma nota, depois outra - vinda da galeria. Lacraram a entrada e deixaram um aviso para não abri-la.")),
 "postcard": (("UNSENT POSTCARD", "CARTÃO-POSTAL NÃO ENVIADO"), [
   ("Dear Lida, it is warm down here. Eleven of us, nine bunks made, because two of the lads never came off shift. I am writing this so that you know I was not afraid. The singing is not so bad once you stop trying to stop it.", "Querida Lida, está quente aqui embaixo. Somos onze, nove beliches arrumadas, porque dois dos rapazes nunca saíram do turno. Escrevo para você saber que eu não tive medo. A cantoria não é tão ruim quando você para de tentar parar de ouvir.")],
  ("The Valley Mine: an unsent postcard from a miner who stopped being afraid of the singing.", "A Mina do Vale: um cartão-postal não enviado de um mineiro que deixou de ter medo da cantoria.")),
 "foreman": (("FOREMAN'S LOGBOOK", "LIVRO DO CAPATAZ"), [
   ("Rams 1 to 4 run on the hour. Rams 3 and 4 stopped on their own at 02:00 and started again on a beat I did not set. Eleven seconds.", "Os êmbolos 1 a 4 funcionam de hora em hora. Os 3 e 4 pararam sozinhos às 02:00 e recomeçaram num ritmo que eu não marquei. Onze segundos."),
   ("I locked the cage. The crew went down the ladder to see. Nobody came back up. Whoever finds this: the vault is sealed because I sealed it, and the money is the least of what is down there.", "Tranquei a gaiola. A equipe desceu a escada para ver. Ninguém voltou a subir. Quem encontrar isto: o cofre está lacrado porque eu o lacrei, e o dinheiro é o de menos que há lá embaixo.")],
  ("The Crusher Pit: the foreman's logbook records rams 3 and 4 restarting by themselves on an eleven-second beat, and a crew that went down the ladder and never came up.",
   "O Poço dos Britadores: o livro do capataz registra os êmbolos 3 e 4 recomeçando sozinhos num ritmo de onze segundos, e uma equipe que desceu a escada e nunca mais subiu.")),
}
def cs_esc(s): return s.replace("\\", "\\\\").replace('"', '\\"')
lines = ["// GENERATED by ArtSource/story/tier2_poi_docs.py - do not edit by hand. The Tier 2 points of interest's papers (English; the Portuguese is in Resources/Lang/pt-BR.txt). Read by Docs.Read.",
         "using System.Collections.Generic;", "", "namespace GameCore", "{", "    public static partial class Tier2Poi", "    {",
         "        public static readonly Dictionary<string, (string title, string[] pages, string journal)> DocTexts = new Dictionary<string, (string, string[], string)>", "        {"]
for k, (t, pages, j) in D.items():
    lines.append('            ["%s"] = ("%s", new[] { %s }, "%s"),' % (k, cs_esc(t[0]), ", ".join('"%s"' % cs_esc(p[0]) for p in pages), cs_esc(j[0])))
lines += ["        };", "    }", "}"]
open(os.path.join(ROOT, "Assets/Scripts/GameCore/Tier2Poi.Docs.cs"), "w").write("\n".join(lines) + "\n")
tab = os.path.join(ROOT, "Assets/Resources/Lang/pt-BR.txt"); cur = open(tab, encoding="utf-8").read()
have = {l.split("\t")[0].lower() for l in cur.split("\n") if "\t" in l}
out = ["", "# ---- Tier 2 points of interest: the papers (ArtSource/story/tier2_poi_docs.py) ----"]
for k, (t, pages, j) in D.items():
    for en, pt in [t] + pages + [j]:
        if en.lower() not in have: out.append(en + "\t" + pt); have.add(en.lower())
if len(out) > 2: open(tab, "a", encoding="utf-8").write("\n".join(out) + "\n")
print(len(D), "papers;", len(out) - 2, "Portuguese lines added")
