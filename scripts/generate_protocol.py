from pathlib import Path
import re
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,Table,TableStyle,PageBreak,KeepTogether
from reportlab.lib.styles import getSampleStyleSheet,ParagraphStyle
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.enums import TA_LEFT
from xml.sax.saxutils import escape
base=Path(__file__).resolve().parents[1]/'docs'
source=base/'protocole-tests-utilisateurs.md'
s=source.read_text(encoding='utf-8-sig').replace('---PAGE---','<!-- pagebreak -->')
source.write_text(s,encoding='utf-8')
import reportlab
font_root=Path('C:/Windows/Fonts')
if font_root.exists():
 for name,file in [('Arial','arial.ttf'),('Arial-Bold','arialbd.ttf')]:pdfmetrics.registerFont(TTFont(name,str(font_root/file)))
else:
 font_root=Path(reportlab.__file__).parent/'fonts'
 for name,file in [('Arial','Vera.ttf'),('Arial-Bold','VeraBd.ttf')]:pdfmetrics.registerFont(TTFont(name,str(font_root/file)))
styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='BodyFr',fontName='Arial',fontSize=9.2,leading=12,spaceAfter=7,textColor=colors.HexColor('#253442')))
styles.add(ParagraphStyle(name='TitleFr',parent=styles['BodyFr'],fontName='Arial-Bold',fontSize=20,leading=24,spaceAfter=14))
styles.add(ParagraphStyle(name='H2Fr',parent=styles['BodyFr'],fontName='Arial-Bold',fontSize=12,leading=15,spaceBefore=9,spaceAfter=7,keepWithNext=True,textColor=colors.HexColor('#086e72')))
styles.add(ParagraphStyle(name='H3Fr',parent=styles['H2Fr'],fontSize=10.5,leading=13,spaceBefore=5))
styles.add(ParagraphStyle(name='CellFr',parent=styles['BodyFr'],fontSize=8.1,leading=10.1,spaceAfter=0))
def fmt(t):
 t=escape(t)
 return re.sub(r'\[([^\]]+)\]\(([^)]+)\)',lambda m:f'<link href="{m[2]}" color="#086e72">{m[1]}</link>',t)
def para(t,style='BodyFr'):return Paragraph(fmt(t),styles[style])
story=[];lines=s.splitlines();i=0
while i<len(lines):
 line=lines[i].strip();i+=1
 if not line:continue
 if line=='<!-- pagebreak -->':story.append(PageBreak());continue
 if line.startswith('# '):story.append(para(line[2:],'TitleFr'));continue
 if line.startswith('## '):story.append(para(line[3:],'H2Fr'));continue
 if line.startswith('### '):story.append(para(line[4:],'H3Fr'));continue
 if line.startswith('|'):
  rows=[line]
  while i<len(lines) and lines[i].strip().startswith('|'):rows.append(lines[i].strip());i+=1
  values=[[c.strip() for c in row.strip('|').split('|')] for row in rows]
  values=[row for row in values if not all(re.fullmatch(r'[: -]+',c) for c in row)]
  columns=len(values[0]);width=511
  if columns==6:widths=[width-5*27]+[27]*5
  elif columns==5:widths=[215,54,65,65,112] if 'Tâche observée' in values[0][0] else [140,105,55,140,71]
  else:widths=[170,210,131]
  table=Table([[para(c,'CellFr') for c in row] for row in values],colWidths=widths,repeatRows=1,hAlign='LEFT')
  table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#e4f0f0')),('GRID',(0,0),(-1,-1),.4,colors.HexColor('#b7c6ca')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),6),('RIGHTPADDING',(0,0),(-1,-1),6),('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),5)]))
  story.extend([table,Spacer(1,8)]);continue
 if line.startswith('- '):story.append(para('• '+line[2:]));continue
 if re.match(r'^\d+\.',line):story.append(para(line));continue
 text=line
 while i<len(lines) and lines[i].strip() and not lines[i].lstrip().startswith(('#','|','- ','<!--')):
  text+=' '+lines[i].strip();i+=1
 story.append(para(text))
def footer(c,doc):
 c.setStrokeColor(colors.HexColor('#c4d5d7'));c.line(42,36,553,36)
 c.setFont('Arial',8);c.setFillColor(colors.HexColor('#526873'));c.drawString(42,24,'Virtual Museum · Protocole de tests utilisateurs');c.drawRightString(553,24,str(doc.page))
doc=SimpleDocTemplate(str(base/'Protocole-tests-utilisateurs.pdf'),pagesize=(595.28,841.89),rightMargin=42,leftMargin=42,topMargin=36,bottomMargin=48,title='Virtual Museum - Protocole de tests utilisateurs',author='Marina, Alexis et Pacôme CAILLETEAU')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
from pypdf import PdfReader
pdf=PdfReader(base/'Protocole-tests-utilisateurs.pdf')
print('Pages:',len(pdf.pages))
for i,page in enumerate(pdf.pages):
 text=page.extract_text()
 print(i+1,len(text),text[:65].replace('\n',' '))
 assert not any(c in text for c in ['—','–',';'])
