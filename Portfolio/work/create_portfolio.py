from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.colors import HexColor

OUT=Path(__file__).resolve().parents[1]
pdfmetrics.registerFont(TTFont('JP','C:/Windows/Fonts/meiryo.ttc',subfontIndex=0))
W,H=595.28,841.89
pages=[
('作品概要',[
('サバイバルホラーゲーム','Studio51株式会社 応募作品\nゲームプログラマー志望　個人制作　制作中\n制作期間　8か月 継続中\nプレイ動画  https://youtu.be/Ea-cbIwZtK4'),
('作品のコンセプト','『SILENT HILL』のようなクラシックなホラーゲームに着想を得た、3Dサバイバルホラーゲームです。エリアごとのカメラ切替とラジコン操作を組み合わせ、周囲を警戒しながら移動するゲーム体験を目指しています。'),
('制作体制と担当','個人制作として開発しています。キャラクター等の素材にはSketchfab、アニメーション素材にはMixamoを使用し、それ以外のゲーム制作を担当しました。主な担当は、C#によるゲーム処理、Cinemachineのカメラ設定、プレイヤー操作、各システムの組み込みです。'),
('開発環境','Unity 6  6000.0.60f1 ／ C#\nCinemachine 3.1.5 ／ Universal Render Pipeline 17.0.4\nAI Navigation 2.0.9'),
('現在の実装','カメラエリアによる視点切替、前後移動と旋回、走行、180度クイックターン、構えと上下照準、射撃とリロード、体力と回復、弾薬取得、敵の追跡と攻撃、体力・弾薬HUDを実装しています。現在は制作途中で、操作性や演出を調整している段階です。'),
('プログラミングでの取り組み','8か月間、ゲーム制作と並行してC#によるプログラミングを学んでいます。学んだ内容をプレイヤー操作、カメラ切替、戦闘、HUDなどの実装に活用しています。本資料では、特にCinemachineの優先度を使ったカメラ切替と、視点に依存しないプレイヤー制御の設計を紹介します。')]),
('Cinemachineによるカメラ制御',[
('狙い','場所ごとに視点を切り替える構成を採用しました。カメラ位置や画角をエリア単位で調整できるため、通路や曲がり角など、場面に応じた見せ方を作り込めます。'),
('実装の流れ','1　エリアにTrigger Colliderを配置する\n2　CamSwitcherに対象のCinemachineCameraを設定する\n3　Playerタグのオブジェクトが入るとPriorityを1にする\n4　エリアから出るとPriorityを0に戻す'),
('カメラ選択の仕組み','CamSwitcherはOnTriggerEnterとOnTriggerExitでプレイヤーの出入りを判定します。カメラ本体を生成・破棄するのではなく、既に配置したカメラの優先度を変更して、Cinemachine側のカメラ選択につなげています。MainGameシーンには複数のカメラ参照を持つ切替エリアが配置されています。'),
('処理の要点','if (other.CompareTag("Player"))\n    activeCamera.Priority = 1;\n\n退出時は同じ判定でPriority = 0に戻します。\n担当スクリプト  CamSwitcher.cs'),
('操作との組み合わせ','移動方向はカメラの向きではなく、プレイヤーのtransform.forwardを基準にしています。視点が切り替わっても「前進はキャラクターの正面へ進む」という入力の意味が変わらない構成です。'),
('今後の改善','複数のエリアが重なった場合に同じ優先度になるため、境界で意図したカメラが選ばれるかを確認し、必要に応じて優先順位の管理を追加します。切替時の見やすさと敵の視認性も調整予定です。')]),
('プレイヤー操作とアニメーション',[
('ラジコン操作の実装','TankControlsで前後入力と左右入力を取得し、左右入力はY軸の旋回、前後入力はキャラクター正面方向への移動に使用しています。CharacterController.Moveで移動を適用し、Time.deltaTimeを使って移動量と旋回量を計算しています。'),
('移動の使い分け','前進中に左Shiftを押すと走行し、後退には別の速度倍率を適用します。コード上の初期倍率は走行1.5倍、後退0.6倍です。これらはInspectorで調整できるため、ホラーゲームに合う移動速度を試しながら設定できます。'),
('180度クイックターン','Qキーでクイックターンを開始します。Coroutine内で開始時と180度回転後のQuaternionをSlerp補間し、反転中は通常移動を止めます。終了後にクールダウンを設け、連続入力を制限しています。'),
('構えと戦闘入力','Spaceキーで構えを切り替えます。構え中は前後移動を止め、左右旋回とW・Sによる上下照準を行います。照準角度は−45度から45度に制限しています。Jまたは左クリックで射撃、KまたはRでリロードを呼び出します。'),
('処理を分けた構成','TankControls　入力と実際の移動・旋回\nMovementState　前進・後退・走行・構えなどの状態整理\nAnimationDriver　状態をAnimatorのパラメータへ反映'),
('素材とゲーム処理の接続','Mixamoのアニメーション素材を使用し、移動状態に応じてAnimatorの真偽値やAimAngleを更新しています。構え中やクイックターン中には他の移動パラメータを抑制し、操作状態とアニメーションを対応させています。')]),
('周辺システムと今後の制作',[
('射撃とリロード','Gunクラスで射撃間隔、装弾数、予備弾数、リロード時間を管理しています。発射時には弾を生成し、アニメーション、効果音、マズルフラッシュ、薬莢排出を呼び出します。リロードは不足弾数と予備弾数を比較して補充します。'),
('敵の行動','ZombieAIでは距離と視野角でプレイヤーを検知し、NavMeshAgentで追跡します。見失った場合は最後に検知した位置へ向かいます。近距離では攻撃を開始し、待機時間後に距離を再判定してダメージを与えます。現在の視覚判定は距離と角度を使用しています。'),
('体力と画面表示','HealthManagerで体力、回復、死亡状態を管理し、イベントで変更を通知します。SurvivalHUDでは体力、装弾数、予備弾数、操作案内、ポーズ、死亡後の再開始を扱います。死亡時は移動や射撃処理を制限しています。'),
('設計上の工夫','入力、状態、アニメーション、武器、体力、カメラ切替をそれぞれのクラスに分けています。例えば武器側に弾薬管理を集めることで、移動処理に射撃の詳細を持たせずに連携させています。'),
('今後取り組むこと','カメラ境界での切替確認、照準と弾の発射方向の整合確認、敵の遮蔽物判定、移動とアニメーションの調整、ゲーム全体の進行と演出の充実を進めます。制作途中のため、完成度を上げながら内容を更新していきます。'),
('使用素材','キャラクター等の素材　Sketchfab\nアニメーション素材　Mixamo\n提出時には使用した各素材の名称、作者、配布元と利用条件を別途整理します。')])]
pages[2][1][3]=('構えと戦闘入力','Spaceキーで構えを切り替え、構え中は肩越しの三人称視点でマウスによる照準操作を行います。通常の移動入力を止め、マウスの左右入力でキャラクターの向き、上下入力で照準角度を変更します。Jまたは左クリックで射撃、KまたはRでリロードを呼び出します。')
pages.insert(3,('友人のフィードバックを受けた改善',[
('課題の発見','友人にプレイしてもらった際、「操作が難しい」というフィードバックを受けました。これを受け、狙いをつけやすくするために、構え中の視点と照準操作を変更しました。'),
('変更前と変更後','変更前　エリアごとのカメラ視点で、キーによる旋回と上下照準を行う方式。\n変更後　探索時は従来のカメラを使用し、構え中は肩越しの三人称視点とマウス照準に切り替える方式。画面中央に照準を表示します。'),
('カメラ制御の切替','ShoulderAimで構え開始時にカメラとカーソルの状態を保存し、CinemachineBrainを一時的に無効化します。構え中は自作処理でカメラ位置と回転を制御します。構え解除時はBrainを元の状態へ戻し、探索用カメラへ補間して復帰します。'),
('マウス照準と画面中央の狙い','Mouse Xで左右の向き、Mouse Yで上下角度を更新し、上下角度には制限を設けています。GetAimPointでは画面中央からRayを飛ばし、プレイヤー自身などを除外して狙う位置を求めます。'),
('切替と障害物への対応','位置・回転・画角を補間して視点を切り替えます。肩位置からSphereCastを行い、障害物までの距離に応じてカメラを近づける処理も追加しています。感度、肩位置、画角、切替時間は調整可能です。'),
('改善を通して意識したこと','自分が操作できることだけで判断せず、他の人のプレイ感想をもとに操作方法を見直しました。今後は変更後の操作について再度フィードバックを集め、狙いやすさや視点切替時の見やすさを確認して調整します。')]))
import json
pages=json.loads((OUT/'work'/'simple_japanese.json').read_text(encoding='utf-8'))
pages[0][1][0]=(pages[0][1][0][0],pages[0][1][0][1].replace('Studio51株式会社 応募作品\n',''))
pages[0][1][0]=(pages[0][1][0][0], '氏名　リヤナゲ　シェナル　ナディジャ\n'+pages[0][1][0][1])
pages[0][1][0]=(pages[0][1][0][0], pages[0][1][0][1]+'\nソースコード  Google Driveからダウンロード')
source_url='https://drive.google.com/file/d/1zbBjax3gF_yrwC0BT0VXfnX9k4P__7d7/view?usp=sharing'
pages.insert(4,('射撃システムの変更前と変更後',[]))
pages.insert(1,('なぜSILENT HILLのようなゲームを作るのか',[
('このゲームを作りたい理由','私は昔から『SILENT HILL』シリーズの、映画のようなカメラの見せ方が好きです。特に『SILENT HILL 1』のカメラは、今まで遊んだホラーゲームの中でも強く印象に残っています。その理由をもっと知りたいと思い、このようなホラーゲームを作り始めました。'),
('好きなカメラのスタイル','私が面白いと思ったのは、カメラが同じ場所に止まっているだけではないことです。プレイヤーの動きに合わせてカメラが動いたり、向きが変わったりします。場所によっては、映画のドリー撮影のように、カメラがなめらかに動くところも好きです。'),
('カメラで感じるこわさ','カメラは、ただプレイヤーを映すためだけのものではないと思います。カメラの位置や動きによって、周りが気になったり、何かが起こりそうだと感じたりします。敵がまだ見えていなくても、不安になるような雰囲気を作れるところに興味があります。'),
('もっと知りたいこと','なぜその場所から映すのか、なぜその動きで不安になるのかを考えたいです。カメラの向きや、画面の中で人物や物をどこに置くかによって、プレイヤーの気持ちがどう変わるのかを学びたいです。こうした映画のような見せ方にも興味があります。'),
('今の制作とこれからの目標','今はCinemachineを使い、場所によってカメラを変える仕組みを作っています。これからは、好きなカメラの動きを調べ、自分でも試して、ゲームに合うように変えていきたいです。同じ仕組みを作るだけでなく、カメラで物語や雰囲気、心に感じるこわさをどう伝えるかを学ぶことが目標です。'),
('次のゲームにも使いたいこと','この制作で学んだことを、これから作るゲームにも使いたいです。遊んだ人の心に長く残るようなゲームを作れるようになりたいです。')]))
c=canvas.Canvas(str(OUT/'Horror_Game_Portfolio_JP.pdf'),pagesize=(W,H))
c.setTitle('サバイバルホラーゲーム 制作ポートフォリオ')
md=['# サバイバルホラーゲーム 制作ポートフォリオ\n\n制作中\n']
def wrap(s,size,width):
    lines=[]
    for part in s.split('\n'):
        line=''
        for ch in part:
            if pdfmetrics.stringWidth(line+ch,'JP',size)>width:
                lines.append(line);line=ch
            else: line+=ch
        lines.append(line)
    return lines
for i,(title,items) in enumerate(pages):
    c.setFillColor(HexColor('#263d42'));c.rect(0,H-12,W,12,fill=1,stroke=0)
    c.setFillColor(HexColor('#526368'));c.setFont('JP',9);c.drawString(45,H-48,'GAME DEVELOPMENT PORTFOLIO  /  制作中')
    c.setFillColor(HexColor('#111111'));c.setFont('JP',17 if pdfmetrics.stringWidth(title,'JP',21)>W-90 else 21);c.drawString(45,H-85,title)
    y=H-118
    md.append('\n## '+title+'\n')
    if title == '射撃システムの変更前と変更後':
        from reportlab.lib.utils import ImageReader
        for label, filename, caption, url in [
            ('変更前','shooting-before.png','遠くから見るカメラで、キーを使って敵を狙っていました。','https://youtu.be/Ea-cbIwZtK4'),
            ('変更後','shooting-after.png','銃を構えると肩越しのカメラに変わり、マウスで狙えます。','https://youtu.be/3X-1d06Ikg0')]:
            c.setFillColor(HexColor('#263d42'));c.setFont('JP',12);c.drawString(45,y,label);y-=16
            img=ImageReader(str(OUT/'images'/filename))
            iw,ih=img.getSize();dw=430;dh=dw*ih/iw
            c.drawImage(img,45,y-dh,width=dw,height=dh);y-=dh+18
            c.setFillColor(HexColor('#252525'));c.setFont('JP',10)
            for line in wrap(caption,10,W-90):
                c.drawString(45,y,line);y-=17
            c.setFont('JP',9);c.drawString(45,y,'動画  '+url)
            c.linkURL(url,(45,y-2,W-45,y+12),relative=0);y-=35
            md.append(f'\n### {label}\n\n![{label}](images/{filename})\n\n{caption}\n\n動画 {url}\n')
    for heading,body in items:
        c.setFillColor(HexColor('#263d42'));c.setFont('JP',12);c.drawString(45,y,heading);y-=21
        c.setFillColor(HexColor('#252525'));c.setFont('JP',10)
        for line in wrap(body,10,W-90):
            c.drawString(45,y,line);y-=17
            if 'https://youtu.be/' in line:
                url=line[line.index('https://youtu.be/'):].strip()
                c.linkURL(url,(45,y+14,W-45,y+30),relative=0)
            if 'ソースコード  Google Drive' in line:
                c.linkURL(source_url,(45,y+14,W-45,y+30),relative=0)
        y-=17
        md.append('\n### '+heading+'\n\n'+body+'\n')
        if 'ソースコード  Google Drive' in body:
            md.append('\n[ソースコードをダウンロード]('+source_url+')\n')
    assert y>45,(i,y)
    c.setFont('JP',8);c.setFillColor(HexColor('#657277'));c.drawString(45,27,'サバイバルホラーゲーム  個人制作');c.drawRightString(W-45,27,f'{i+1} / {len(pages)}')
    c.showPage()
c.save()
(OUT/'Horror_Game_Portfolio_JP.md').write_text('\n'.join(md),encoding='utf-8')
print('Created',len(pages),'pages')
