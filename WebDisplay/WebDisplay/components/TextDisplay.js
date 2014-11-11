function TextDisplay()
{
    this.data = {};

    this.init = function ()
    {
        if (this.element == null || this.element == undefined)
        {
            this.element = document.createElement("div");
            this.element.style.position = 'absolute';
            this.element.style.left = this.data.X + "px";
            this.element.style.top = this.data.Y + "px";
            /*this.element.style.width = this.data.Width + "px";
            this.element.style.height = this.data.Height + "px";*/
            this.element.innerHTML = this.data.Text;
            this.element.style.color = this.data.Color;
            document.getElementById("displayArea").appendChild(this.element);
        }
    }

    this.disconnect = function ()
    {
    }

    this.update = function (channel, value)
    {
    }
}