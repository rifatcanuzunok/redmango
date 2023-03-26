const withAuth = (WrappedComponent: any) => {
  return (props: any) => {
    const access_token = localStorage.getItem("token");
    if (!access_token) {
      window.location.replace("/login");
      return null;
    }
    return <WrappedComponent {...props} />;
  };
};

export default withAuth;
