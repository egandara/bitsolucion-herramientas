# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_019_Base_Normativa_Operaciones_Proceso_V2
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ***************************************************************************************************************
# MAGIC * Nombre: BCI_019_Base_Normativa_Operaciones_Proceso_V2.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones boleta garantia
# MAGIC * Documentación: 
# MAGIC ***************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_com_gr
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_d00_v3_act_ctg

# COMMAND ----------

drop_d00_v3_act_ctg  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_d00_v3_act_ctg"""

# COMMAND ----------

sql_safe(drop_d00_v3_act_ctg)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_d00_v3_act_ctg", True)

# COMMAND ----------

create_d00_v3_act_ctg  = """CREATE TABLE """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
(
  periodo_id            INT         COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente           INT         COMMENT 'RUT del Cliente Titular',
  dv_cliente            STRING      COMMENT 'Digito verificador del cliente titular',
  cod_num_operacion     STRING      COMMENT 'Número Operación (OPE_VIGENTE)',
  cod_ope_original      STRING      COMMENT 'Operación Original (OPE_ORIGINAL)',
  cod_tipo_ope          STRING      COMMENT 'Tipo-subtipo del D00',
  cod_cctb              STRING      COMMENT 'Cuenta contable',
  cod_tipo_activo       INT         COMMENT 'Tipo Activo Cartera',
  cod_calif             STRING      COMMENT 'Calificacion Cliente GR/A1/B1/C4',
  cod_cartera_ope       STRING      COMMENT 'Subtipo cartera',
  fec_proceso           DATE        COMMENT 'Fecha de cierre' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_d00_v3_act_ctg' """

# COMMAND ----------

sql_safe(create_d00_v3_act_ctg)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_arm_d00_v3_act

# COMMAND ----------

drop_arm_d00_v3_act  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_arm_d00_v3_act"""

# COMMAND ----------

sql_safe(drop_arm_d00_v3_act)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_arm_d00_v3_act", True)

# COMMAND ----------

create_arm_d00_v3_act  = """CREATE TABLE """ + db_plat_tempX + """.tmp_arm_d00_v3_act
(
  periodo_id            INT         COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente           INT         COMMENT 'RUT del Cliente Titular',
  dv_cliente            STRING      COMMENT 'Digito verificador del cliente titular',
  cod_num_operacion     STRING      COMMENT 'Número Operación (OPE_VIGENTE)',
  cod_ope_original      STRING      COMMENT 'Operación Original (OPE_ORIGINAL)',
  cod_tipo_ope          STRING      COMMENT 'Tipo-subtipo del D00',
  cod_cctb              STRING      COMMENT 'Cuenta contable',
  cod_tipo_activo       INT         COMMENT 'Tipo Activo Cartera',
  cod_calif             STRING      COMMENT 'Calificacion Cliente GR/A1/B1/C4',
  cod_cartera_ope       STRING      COMMENT 'Subtipo cartera',
  fec_proceso           DATE        COMMENT 'Fecha de cierre' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_arm_d00_v3_act' """

# COMMAND ----------

sql_safe(create_arm_d00_v3_act)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_arm_d00_v3_act_2

# COMMAND ----------

drop_arm_d00_v3_act_2  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_arm_d00_v3_act_2"""

# COMMAND ----------

sql_safe(drop_arm_d00_v3_act_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_arm_d00_v3_act_2", True)

# COMMAND ----------

create_arm_d00_v3_act_2  = """CREATE TABLE """ + db_plat_tempX + """.tmp_arm_d00_v3_act_2
(
  periodo_id            INT         COMMENT 'Periodo de ejecucion SSAAMM',
  cod_ope_original      STRING      COMMENT 'Operación Original (OPE_ORIGINAL)',
  cantidad              INT         COMMENT 'Cantidad de operaciones',
  fec_proceso           DATE        COMMENT 'Fecha de cierre' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_arm_d00_v3_act_2' """

# COMMAND ----------

sql_safe(create_arm_d00_v3_act_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_arm_d00_v3_act_3

# COMMAND ----------

drop_arm_d00_v3_act_3  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_arm_d00_v3_act_3"""

# COMMAND ----------

sql_safe(drop_arm_d00_v3_act_3)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_arm_d00_v3_act_3", True)

# COMMAND ----------

create_arm_d00_v3_act_3  = """CREATE TABLE """ + db_plat_tempX + """.tmp_arm_d00_v3_act_3
(
  periodo_id            INT         COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente           INT         COMMENT 'RUT del Cliente Titular',
  dv_cliente            STRING      COMMENT 'Digito verificador del cliente titular',
  cod_num_operacion     STRING      COMMENT 'Número Operación (OPE_VIGENTE)',
  cod_ope_original      STRING      COMMENT 'Operación Original (OPE_ORIGINAL)',
  cod_tipo_ope          STRING      COMMENT 'Tipo-subtipo del D00',
  cod_cctb              STRING      COMMENT 'Cuenta contable',
  cod_tipo_activo       INT         COMMENT 'Tipo Activo Cartera',
  cod_calif             STRING      COMMENT 'Calificacion Cliente GR/A1/B1/C4',
  cod_cartera_ope       STRING      COMMENT 'Subtipo cartera',
  fec_proceso           DATE        COMMENT 'Fecha de cierre' 
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_arm_d00_v3_act_3' """

# COMMAND ----------

sql_safe(create_arm_d00_v3_act_3)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 1: Obtencion Univ Operaciones Cartera Activa

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con cartera activa desde base_normativa_v3_ft
# MAGIC
# MAGIC **Tabla de salida**: tmp_d00_v3_act_ctg (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 		
# MAGIC - **rut_cliente**: Rut del cliente 		
# MAGIC - **dv_cliente**: Digito verificador del cliente				
# MAGIC - **cod_num_operacion**: Codigo de operacion
# MAGIC - **cod_ope_original**: Codigo operacion original
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00	
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **cod_calif**: Numero de operacion	
# MAGIC - **cod_cartera_ope**: Subtipo cartera		
# MAGIC - **fec_proceso**: Fecha de cierre	

# COMMAND ----------

vista_d00_v3_act_ctg = """ insert into """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  select  periodo_id,          
          rut_cliente,
          dv_cliente,       
          cod_num_operacion, 
          cod_ope_original,
          cod_tipo_ope,      
          cod_cctb,          
          cod_tabla_89            as cod_tipo_activo,   
          cod_calif, 
          cod_cartera_ope,     
          '""" + date_proceso + """'        as fec_proceso  
  from """ + db_platinumX + """.base_archivos_normativos
  where fec_proceso = '""" + date_proceso + """'
    and trim(cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_d00_v3_act_ctg)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 2: Obtencion Univ Operaciones Cartera Contingente

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con cartera contingente desde base_normativa_v3_ft
# MAGIC
# MAGIC **Tabla de salida**: tmp_d00_v3_act_ctg (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 		
# MAGIC - **rut_cliente**: Rut del cliente 		
# MAGIC - **dv_cliente**: Digito verificador del cliente				
# MAGIC - **cod_num_operacion**: Codigo de operacion
# MAGIC - **cod_ope_original**: Codigo operacion original
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00	
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **cod_calif**: Numero de operacion	
# MAGIC - **cod_cartera_ope**: Subtipo cartera		
# MAGIC - **fec_proceso**: Fecha de cierre	

# COMMAND ----------

vista_d00_v3_act_ctg_2 = """ insert into """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  select  periodo_id,          
          rut_cliente,
          dv_cliente,       
          cod_num_operacion, 
          cod_ope_original,
          cod_tipo_ope,      
          cod_cctb,          
          cod_tabla_89              as cod_tipo_activo,   
          cod_calif, 
          cod_cartera_ope,     
          '""" + date_proceso + """'        as fec_proceso       
  from """ + db_platinumX + """.base_archivos_normativos
  where fec_proceso = '""" + date_proceso + """'
    and trim(cod_cartera_ope) = 'CTG'
"""

# COMMAND ----------

sqlsafe(vista_d00_v3_act_ctg_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 3: Obtencion Univ Operaciones Activas Postergacion Hipotecarias

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con cartera activa contables desde base_normativa_v3_ft
# MAGIC
# MAGIC **Tabla de salida**: tmp_arm_d00_v3_act (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 		
# MAGIC - **rut_cliente**: Rut del cliente 						
# MAGIC - **cod_num_operacion**: Numero de operacion
# MAGIC - **cod_ope_original**: Operación Original
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00	
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **cod_calif**: Numero de operacion	
# MAGIC - **cod_cartera_ope**: Subtipo cartera		
# MAGIC - **fec_proceso**: Fecha de cierre	

# COMMAND ----------

vista_arm_d00_v3_act = """ insert into """ + db_plat_tempX + """.tmp_arm_d00_v3_act
  select  periodo_id,          
          rut_cliente,
          dv_cliente,       
          cod_num_operacion,
          cod_ope_original, 
          cod_tipo_ope,      
          cod_cctb,          
          cod_tabla_89              as cod_tipo_activo,   
          cod_calif, 
          cod_cartera_ope,     
          '""" + date_proceso + """'        as fec_proceso       
  from """ + db_platinumX + """.base_archivos_normativos
  where fec_proceso = '""" + date_proceso + """'
    and trim(cod_cartera_ope) = 'ACT'
    and trim(cod_cctb) in ('124611080', '141110032')
"""

# COMMAND ----------

sqlsafe(vista_arm_d00_v3_act)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 4: Obtencion Cantidad Operaciones Duplicadas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de la cantidad de operaciones duplicadas desde tmp_arm_d00_v3_act
# MAGIC
# MAGIC **Tabla de salida**: tmp_arm_d00_v3_act_2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 
# MAGIC - **cod_ope_original**: Operación Original	
# MAGIC - **cantidad**: Cantidad de operaciones		
# MAGIC - **fec_proceso**: Fecha de cierre	

# COMMAND ----------

vista_arm_d00_v3_act_2 = """ insert into """ + db_plat_tempX + """.tmp_arm_d00_v3_act_2
  select  periodo_id,          
          cod_ope_original, 
          count(*)                          as cantidad,   
          '""" + date_proceso + """'        as fec_proceso       
  from """ + db_plat_tempX + """.tmp_arm_d00_v3_act
  where fec_proceso = '""" + date_proceso + """'
  group by periodo_id, cod_ope_original
  having count(*) > 1
"""

# COMMAND ----------

sqlsafe(vista_arm_d00_v3_act_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 5: Obtencion Operaciones Prestamos Consumo Hipotecarias

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con prestamos de consumo hipotecarias desde tmp_arm_d00_v3_act y tmp_arm_d00_v3_act_2
# MAGIC
# MAGIC **Tabla de salida**: tmp_arm_d00_v3_act_3 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 		
# MAGIC - **rut_cliente**: Rut del cliente 						
# MAGIC - **cod_num_operacion**: Numero de operacion
# MAGIC - **cod_ope_original**: Operación Original
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00	
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_tipo_activo**: Tipo Activo Cartera
# MAGIC - **cod_calif**: Numero de operacion	
# MAGIC - **cod_cartera_ope**: Subtipo cartera		
# MAGIC - **fec_proceso**: Fecha de cierre	

# COMMAND ----------

vista_arm_d00_v3_act_3 = """ insert into """ + db_plat_tempX + """.tmp_arm_d00_v3_act_3
  select  a.periodo_id,          
          a.rut_cliente,   
          a.dv_cliente,    
          a.cod_num_operacion,
          b.cod_ope_original, 
          a.cod_tipo_ope,      
          a.cod_cctb,          
          a.cod_tipo_activo,   
          a.cod_calif, 
          a.cod_cartera_ope,     
          '""" + date_proceso + """'        as fec_proceso       
  from """ + db_plat_tempX + """.tmp_arm_d00_v3_act a
  join """ + db_plat_tempX + """.tmp_arm_d00_v3_act_2 b
    on a.cod_ope_original = b.cod_ope_original
      and a.periodo_id = b.periodo_id
  where a.fec_proceso = '""" + date_proceso + """'
    and trim(a.cod_cctb) = '141110032'
    and trim(a.cod_tipo_ope) = 'HIP300'
"""

# COMMAND ----------

sqlsafe(vista_arm_d00_v3_act_3)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Conteo de registros insertados en tabla de salida

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_d00_v3_act_ctg

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg  where fec_proceso = '""" + date_proceso + """' """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_d00_v3_act_ctg")

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_arm_d00_v3_act

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.tmp_arm_d00_v3_act  where fec_proceso = '""" + date_proceso + """' """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_arm_d00_v3_act")

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_arm_d00_v3_act_2

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.tmp_arm_d00_v3_act_2  where fec_proceso = '""" + date_proceso + """' """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_arm_d00_v3_act_2")

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_arm_d00_v3_act_3

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.tmp_arm_d00_v3_act_3  where fec_proceso = '""" + date_proceso + """' """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_arm_d00_v3_act_3")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")