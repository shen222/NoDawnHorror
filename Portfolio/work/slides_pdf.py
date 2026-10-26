from pathlib import Path
from reportlab.pdfgen import canvas
root=Path(__file__).resolve().parents[1]
c=canvas.Canvas(str(root/'slides/Game_Portfolio_JP_Detailed.pdf'),pagesize=(960,540))
c.setTitle('ゲーム制作ポートフォリオ リヤナゲ シェナル ナディジャ')
for i in range(1,19):
 c.drawImage(str(root/f'work/slides/slide-{i}.png'),0,0,width=960,height=540)
 if i==18:
  c.linkURL('https://youtu.be/Ea-cbIwZtK4',(45,360,900,430),relative=0)
  c.linkURL('https://youtu.be/3X-1d06Ikg0',(45,260,900,340),relative=0)
  c.linkURL('https://drive.google.com/file/d/1zbBjax3gF_yrwC0BT0VXfnX9k4P__7d7/view?usp=sharing',(45,45,900,170),relative=0)
 c.showPage()
c.save()
