using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        enum NoticeLevel { Info, Success, Warning }
        readonly Queue<(string Time,string Message,NoticeLevel Level)> notices=new();
        NoticeLevel noticeLevel;
        bool confirmEmpty;

        void Tell(string message)
        {
            var lower=message.ToLowerInvariant();
            var blocked=new[]{"impossible","échoué","échec","annul","choisis","termine","d’abord","aucune","trop","ferait","occupé","prévois","approche","dépose","valide ou","repose"};
            noticeLevel=blocked.Any(lower.Contains)?NoticeLevel.Warning:
                new[]{"appliqu","créé","validée","replacée","sauvegard","restaur","reliées","retirée","supprim","placée","déplacée","terminé"}.Any(lower.Contains)?NoticeLevel.Success:NoticeLevel.Info;
            status=message;
            notices.Enqueue((DateTime.Now.ToString("HH:mm:ss"),message,noticeLevel));
            while(notices.Count>10)notices.Dequeue();
            WorldAction.LastPressed?.Feedback(noticeLevel==NoticeLevel.Warning?Red:Teal);
            dirty=true;
        }

        void Reject(string message) {Tell(message);noticeLevel=NoticeLevel.Warning;WorldAction.LastPressed?.Feedback(Red);}

        void DrawTableFeedback(Transform controls)
        {
            if(string.IsNullOrEmpty(status))return;
            Text(controls,Wrap(status,65,2),new Vector3(-1.87f,-.32f,-.08f),.053f,
                noticeLevel==NoticeLevel.Warning?Gold:Color.white);
        }

        void DrawTerminal()
        {
            if(!workshop.terminal)return;
            var terminal=WorkshopContent(workshop.terminal);
            Text(terminal,"JOURNAL DE L’ATELIER",new Vector3(-1.84f,.69f,-.08f),.10f,Color.white);
            Text(terminal,"● Action   ● Résultat   ● Attention",new Vector3(-1.84f,.47f,-.08f),.067f,Quiet);
            int line=0;
            foreach(var entry in notices.Reverse().Take(7))
            {
                var color=entry.Level==NoticeLevel.Warning?Gold:entry.Level==NoticeLevel.Success?Teal:Color.white;
                var mark=entry.Level==NoticeLevel.Warning?"!":entry.Level==NoticeLevel.Success?"+":"·";
                Text(terminal,Wrap(entry.Time+"  "+mark+" "+entry.Message,78,2),
                    new Vector3(-1.84f,.24f-line*.19f,-.08f),.059f,color);
                line++;
            }
        }

        void ResetEmptyMuseum()
        {
            if(!WorkshopReady())return;
            if(!confirmEmpty){confirmEmpty=true;Tell("Confirme Nouvelle maquette vide. La sauvegarde sur l’appareil est conservée.");return;}
            Api.Replace(new Museum(),new VisitorSession());
            visitHistory.Clear();
            selectedRoom=selectedArt=selectedTour=linkFirstRoom=null;
            linking=drawing=confirmDelete=confirmEmpty=false;
            corner=null;activeField=TextField.None;
            Tell("Maquette vide. Trace la première salle sur la table.");
        }

        void HighlightArtwork(GameObject visual,bool active)
        {
            var existing=visual.transform.Find("Œuvre repérée");
            if(existing){existing.gameObject.SetActive(active);return;}
            if(!active)return;
            var collider=visual.GetComponent<Collider>();
            var bounds=collider?collider.bounds:new Bounds(visual.transform.position,Vector3.one*.03f);
            var outline=Root(visual.transform,"Œuvre repérée",Vector3.zero);
            var extent=bounds.extents+Vector3.one*.008f;
            for(int axis=0;axis<3;axis++)
                foreach(var a in new[]{-1f,1f})foreach(var b in new[]{-1f,1f})
                {
                    int next=(axis+1)%3,last=(axis+2)%3;
                    var start=bounds.center;start[axis]-=extent[axis];start[next]+=extent[next]*a;start[last]+=extent[last]*b;
                    var end=start;end[axis]+=extent[axis]*2f;
                    Line(outline,start,end,.0035f,Gold);
                }
        }
    }
}
