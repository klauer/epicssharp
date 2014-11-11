function TextMonitorDisplay()
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
            if (this.data.Align == "right")
                this.element.style.textAlign = "right";
            this.data.Precision = parseInt(this.data.Precision);
            document.getElementById("displayArea").appendChild(this.element);
        }
    }

    this.disconnect = function ()
    {
        this.element.innerHTML = "---";
    }

    this.update = function (channel, value)
    {
        if (channel == this.data.ChannelName)
        {
            if (value.indexOf('.') != -1)
            {
                if (this.data.Precision == 0)
                    value = Math.round(parseFloat(value));
                else
                {
                    var prec = Math.pow(10, this.data.Precision);
                    value = Math.round(parseFloat(value) * prec) / prec;
                }
            }
            this.element.innerHTML = value;
        }
    }
}