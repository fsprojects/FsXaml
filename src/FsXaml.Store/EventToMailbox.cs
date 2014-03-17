using System;
using System.Reflection;
using Windows.UI.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FsXaml
{
    public class EventToMailbox : TriggerAction<FrameworkElement>
    {
        internal static IEventArgsConverter DefaultConverter = new DefaultEventConverter();

        public static DependencyProperty MailboxParameterProperty = DependencyProperty.Register("MailboxParameter", typeof(object), typeof(EventToMailbox), new PropertyMetadata(null));
        public static DependencyProperty MailboxProperty = DependencyProperty.Register("Mailbox", typeof(object), typeof(EventToMailbox), new PropertyMetadata(null));
        public static DependencyProperty EventArgsConverterParameterProperty = DependencyProperty.Register("EventArgsConverterParameter", typeof(object), typeof(EventToMailbox), new PropertyMetadata(null));
        public static DependencyProperty EventArgsConverterProperty = DependencyProperty.Register("EventArgsConverter", typeof(object), typeof(EventToMailbox), new PropertyMetadata(DefaultConverter));

        protected override void Invoke(object param)
        {
            object mailbox = this.Mailbox;
            Control associatedControl = this.AssociatedObject as Control;

            bool enable = (associatedControl == null && this.AssociatedObject != null) || associatedControl.IsEnabled;
            if (enable && mailbox != null)
            {
                var parameter = this.EventArgsConverter.Convert(param as RoutedEventArgs, this.EventArgsConverterParameter);

                var mailboxType = mailbox.GetType();
                var parameterType = mailboxType.GenericTypeArguments[0];
                MethodInfo method = mailbox.GetType().GetRuntimeMethod("Post", new[] {parameterType});
                try
                {
                    object[] parameters = { parameter };
                    method.Invoke(mailbox, parameters);
                }
                catch
                {
                }
            }
        }

        public object Mailbox
        {
            get
            {
                return this.GetValue(MailboxProperty);
            }
            set
            {
                this.SetValue(MailboxProperty, value);
            }
        }

        public object MailboxParameter
        {
            get
            {
                return this.GetValue(MailboxParameterProperty);
            }
            set
            {
                this.SetValue(MailboxParameterProperty, value);
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

        public bool PassEventArgsToMailbox { get; set; }

        private class DefaultEventConverter : IEventArgsConverter
        {
            public object Convert(RoutedEventArgs e, object parameter)
            {
                return e;
            }
        }
    }
}