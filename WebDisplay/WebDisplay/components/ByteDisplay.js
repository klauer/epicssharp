function ByteDisplay()
{
    this.data = {};

    this.init = function ()
    {
        conn.server.registerChannel(this.data.ChannelName);
        if (this.element == null || this.element == undefined)
        {
            this.element = document.createElement("div");
            this.element.style.position = 'absolute';
            this.element.style.left = this.data.X + "px";
            this.element.style.top = this.data.Y + "px";
            this.element.style.width = this.data.Width + "px";
            this.element.style.height = this.data.Height + "px";
            this.element.style.border = "solid 1px black";
            this.element.style.backgroundColor = "white";
            document.getElementById("displayArea").appendChild(this.element);
        }
    }

    this.disconnect = function ()
    {
        this.element.innerHTML = "-";
        this.element.style.backgroundColor = "white";
    }

    this.update = function (channel, value)
    {
        this.element.innerHTML = "";
        if (channel == this.data.ChannelName)
        {
            if (this.data.OkValue != value)
                this.element.style.backgroundColor = this.data.OkColor;
            else
                this.element.style.backgroundColor = this.data.ErrorColor;
        }
    }
}