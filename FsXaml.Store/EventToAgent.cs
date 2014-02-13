using System;
using System.Reflection;
using Windows.UI.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FsXaml
{
    public class EventToAgent : TriggerAction<FrameworkElement>
    {
        internal static IEventArgsConverter DefaultConverter = new DefaultEventConverter();

        public static DependencyProperty AgentParameterProperty = DependencyProperty.Register("AgentParameter", typeof(object), typeof(EventToAgent), new PropertyMetadata(null));
        public static DependencyProperty AgentProperty = DependencyProperty.Register("Agent", typeof(object), typeof(EventToAgent), new PropertyMetadata(null));
        public static DependencyProperty EventArgsConverterParameterProperty = DependencyProperty.Register("EventArgsConverterParameter", typeof(object), typeof(EventToAgent), new PropertyMetadata(null));
        public static DependencyProperty EventArgsConverterProperty = DependencyProperty.Register("EventArgsConverter", typeof(object), typeof(EventToAgent), new PropertyMetadata(DefaultConverter));

        protected override void Invoke(object param)
        {
            object agent = this.Agent;
            Control associatedControl = this.AssociatedObject as Control;

            bool enable = (associatedControl == null && this.AssociatedObject != null) || associatedControl.IsEnabled;
            if (enable && agent != null)
            {
                var parameter = this.EventArgsConverter.Convert(param as RoutedEventArgs, this.EventArgsConverterParameter);

                var agentType = agent.GetType();
                var parameterType = agentType.GenericTypeArguments[0];
                MethodInfo method = agent.GetType().GetRuntimeMethod("Post", new[] {parameterType});
                try
                {
                    object[] parameters = { parameter };
                    method.Invoke(agent, parameters);
                }
                catch
                {
                }
            }
        }

        public object Agent
        {
            get
            {
                return this.GetValue(AgentProperty);
            }
            set
            {
                this.SetValue(AgentProperty, value);
            }
        }

        public object AgentParameter
        {
            get
            {
                return this.GetValue(AgentParameterProperty);
            }
            set
            {
                this.SetValue(AgentParameterProperty, value);
            }
        }

        public IEventArgsConverter EventArgsConverter
        {
            get
            {
                return (IEventArgsConverter)this.GetValue(EventArgsConverterProperty);
            }
            set
            {
                this.SetValue(EventArgsConverterProperty, value);
            }
        }

        public object EventArgsConverterParameter
        {
            get
            {
                return this.GetValue(EventArgsConverterParameterProperty);
            }
            set
            {
                this.SetValue(EventArgsConverterParameterProperty, value);
            }
        }

        public bool PassEventArgsToAgent { get; set; }

        private class DefaultEventConverter : IEventArgsConverter
        {
            public object Convert(RoutedEventArgs e, object parameter)
            {
                return e;
            }
        }
    }
}