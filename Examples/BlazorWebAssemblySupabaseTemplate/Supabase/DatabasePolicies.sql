-- CREATE POLICY "policy_name"
-- ON public.Lista FOR
-- DELETE USING ( auth.uid() = user_id );

-- CREATE POLICY "policy_name"
-- ON public.Lista FOR
-- SELECT  USING ( auth.uid() = user_id );

-- #################### TABELA LISTA

-- CREATE POLICY "Enable read access for all users" ON "public"."Lista"
-- AS PERMISSIVE FOR SELECT
-- TO authenticated
-- USING (true)


CREATE POLICY "Users can SELECT if own row"
    ON "public"."TodoPrivate" AS PERMISSIVE FOR SELECT
    TO authenticated
    USING ( auth.uid() = user_id );

CREATE POLICY "Users can UPDATE if own row"
    ON "public"."TodoPrivate" AS PERMISSIVE FOR UPDATE
    TO authenticated
    USING ( auth.uid() = user_id );

CREATE POLICY "Users can DELETE if own row"
    ON "public"."TodoPrivate" AS PERMISSIVE FOR DELETE
    TO authenticated
    USING ( auth.uid() = user_id );

CREATE POLICY "Users can SELECT if authenticated"
    ON "public"."TodoPrivate" 
    AS PERMISSIVE
    FOR INSERT
    TO authenticated
    WITH CHECK (true);