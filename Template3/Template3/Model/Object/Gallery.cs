using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Template3.Model.Object
{
    public class Gallery<T>
    {
        private T[] images;         //Array de imagenes
        private int QtyImage;       //Total de imagenes del array
        private string folder;      //Ubicacion de las imagenes
        private T thumbnail;        //Miniatura para el menu superior
        public Gallery()
        {

        }
        public Gallery(string folder, int cantImg, T thumbnail, T[] images)
        {
            this.folder = folder;
            this.images = images;
            this.thumbnail = thumbnail;
            this.images = images;
        }
        public void setFolder(string folder)
        {
            this.folder = folder;
        }
        public string getFolder()
        {
            return this.folder;
        }
        public void setQtyImage(int cantImg)
        {
            this.QtyImage = cantImg;
        }
        public int getQtyImages()
        {
            return this.QtyImage;
        }
        public void setThumbnail(T thumbnail)
        {
            this.thumbnail = thumbnail;
        }
        public T getThumbnail()
        {
            return this.thumbnail;
        }
        public void setImages(T[] imagenes)
        {
            this.images = imagenes;
        }
        public T[] getImages()
        {
            return this.images;
        }
    }
}
